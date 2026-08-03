using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Infra.Redis.Cache;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Application.Dtos;
using ST.MS.FileUpload.Application.IServices;
using ST.MS.FileUpload.Domain.Entities;
using ST.MS.FileUpload.Domain.Enums;
using ST.MS.FileUpload.Domain.Services;
using ST.MS.FileUpload.Infra.DbContext;
using ST.Shared.Exceptions;

namespace ST.MS.FileUpload.Application.Services;

/// <summary>
/// 分片上传服务实现。
/// 使用 Redis Set 记录已上传分片，提升断点续传查询性能。
/// </summary>
public sealed class MultipartUploadService : IMultipartUploadService
{
	private readonly FileUploadDbContext _dbContext;
	private readonly IFileStorageService _fileStorageService;
	private readonly IRedisCacheManager _redisCacheManager;
	private readonly FileStorageOptions _storageOptions;
	private readonly ILogger<MultipartUploadService> _logger;

	public MultipartUploadService(
		FileUploadDbContext dbContext,
		IFileStorageService fileStorageService,
		IRedisCacheManager redisCacheManager,
		IOptions<FileStorageOptions> storageOptions,
		ILogger<MultipartUploadService> logger)
	{
		_dbContext = dbContext;
		_fileStorageService = fileStorageService;
		_redisCacheManager = redisCacheManager;
		_storageOptions = storageOptions.Value;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task<InitUploadResultDto> InitUploadAsync(InitUploadRequestDto request, Guid userId, string? userName)
	{
		// 参数校验
		if (string.IsNullOrWhiteSpace(request.FileName))
			throw new BusinessException("文件名不能为空");

		if (request.FileSize <= 0)
			throw new BusinessException("文件大小必须大于 0");

		if (request.ChunkSize < 1024 * 1024 || request.ChunkSize > 500 * 1024 * 1024)
			throw new BusinessException("分片大小必须在 1MB 到 500MB 之间");

		// 文件类型校验（与普通上传共用白名单配置）
		var extension = Path.GetExtension(request.FileName);
		if (_storageOptions.AllowedExtensions is { Length: > 0 }
		    && !_storageOptions.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
			throw new BusinessException($"不允许的文件扩展名: {extension}");

		if (!string.IsNullOrWhiteSpace(request.ContentType)
		    && _storageOptions.AllowedContentTypes is { Length: > 0 }
		    && !_storageOptions.AllowedContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
			throw new BusinessException($"不允许的文件类型: {request.ContentType}");

		if (request.FileSize > _storageOptions.MaxFileSize)
			throw new BusinessException($"文件大小超过限制: {_storageOptions.MaxFileSize / 1024 / 1024}MB");

		// 计算总分片数
		var totalChunks = FileUploadSession.CalculateTotalChunks(request.FileSize, request.ChunkSize);

		// 创建上传会话
		var session = new FileUploadSession
		{
			FileName = request.FileName,
			FileHash = request.FileHash,
			FileSize = request.FileSize,
			ChunkSize = request.ChunkSize,
			TotalChunks = totalChunks,
			UploadedChunks = 0,
			Status = UploadStatus.Uploading,
			AccessLevel = (FileAccessLevel)request.AccessLevel,
			CreatedBy = userId,
			CreatorName = userName,
			ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10) // 10 分钟过期
		};

		_dbContext.UploadSessions.Add(session);
		await _dbContext.SaveChangesAsync();

		// 初始化 Redis Set（设置 10 分钟过期）
		var redisKey = GetChunksRedisKey(session.Id);
		await _redisCacheManager.SetStringAsync($"{redisKey}:init", "1", TimeSpan.FromMinutes(10));

		_logger.LogInformation("Initialized multipart upload {UploadId} for file {FileName}, total chunks: {TotalChunks}",
			session.Id, session.FileName, session.TotalChunks);

		return new InitUploadResultDto
		{
			UploadId = session.Id,
			FileName = session.FileName,
			FileSize = session.FileSize,
			ChunkSize = session.ChunkSize,
			TotalChunks = session.TotalChunks,
			Status = session.Status.ToString(),
			ExpiresAtUtc = session.ExpiresAtUtc
		};
	}

	/// <inheritdoc />
	public async Task<UploadStatusDto> GetUploadStatusAsync(Guid uploadId)
	{
		var session = await _dbContext.UploadSessions
			.FirstOrDefaultAsync(s => s.Id == uploadId);

		if (session is null)
			throw new BusinessException("上传会话不存在");

		// 优先从 Redis 读取已上传分片
		var uploadedIndexes = await GetUploadedChunkIndexesFromRedisAsync(uploadId);

		// 如果 Redis 中没有数据，回退到数据库查询
		if (uploadedIndexes.Count == 0 && session.UploadedChunks > 0)
		{
			uploadedIndexes = await _dbContext.UploadChunks
				.Where(c => c.UploadId == uploadId)
				.Select(c => c.ChunkIndex)
				.OrderBy(i => i)
				.ToListAsync();

			// 同步到 Redis
			await SyncChunksToRedisAsync(uploadId, uploadedIndexes);
		}

		var allIndexes = Enumerable.Range(0, session.TotalChunks).ToList();
		var missingIndexes = allIndexes.Except(uploadedIndexes).ToList();

		return new UploadStatusDto
		{
			UploadId = session.Id,
			FileName = session.FileName,
			FileSize = session.FileSize,
			TotalChunks = session.TotalChunks,
			UploadedChunks = uploadedIndexes.Count,
			UploadedChunkIndexes = uploadedIndexes,
			MissingChunkIndexes = missingIndexes,
			Status = session.Status.ToString(),
			FileId = session.FileId
		};
	}

	/// <inheritdoc />
	public async Task UploadChunkAsync(Guid uploadId, int chunkIndex, Stream stream, string? chunkHash)
	{
		var session = await _dbContext.UploadSessions
			.FirstOrDefaultAsync(s => s.Id == uploadId);

		if (session is null)
			throw new BusinessException("上传会话不存在");

		if (session.Status != UploadStatus.Uploading)
			throw new BusinessException($"上传会话状态为 {session.Status}，无法上传分片");

		if (chunkIndex < 0 || chunkIndex >= session.TotalChunks)
			throw new BusinessException($"分片序号 {chunkIndex} 超出范围 [0, {session.TotalChunks - 1}]");

		// 检查分片是否已上传（幂等）- 优先检查 Redis
		var redisKey = GetChunksRedisKey(uploadId);
		var alreadyUploaded = await _redisCacheManager.ExistsAsync(redisKey);

		if (alreadyUploaded)
		{
			// 检查具体分片是否在 Set 中
			var db = _redisCacheManager.GetDatabase();
			var exists = await db.SetContainsAsync(redisKey, chunkIndex.ToString());
			if (exists)
			{
				_logger.LogInformation("Chunk {ChunkIndex} already uploaded for upload {UploadId} (from Redis), skipping",
					chunkIndex, uploadId);
				return;
			}
		}
		else
		{
			// 回退到数据库检查
			var existingChunk = await _dbContext.UploadChunks
				.FirstOrDefaultAsync(c => c.UploadId == uploadId && c.ChunkIndex == chunkIndex);

			if (existingChunk is not null)
			{
				_logger.LogInformation("Chunk {ChunkIndex} already uploaded for upload {UploadId} (from DB), skipping",
					chunkIndex, uploadId);
				// 同步到 Redis
				await AddChunkToRedisAsync(uploadId, chunkIndex);
				return;
			}
		}

		// 存储分片文件
		var chunkFileName = $"{uploadId}/{chunkIndex:D6}.chunk";
		var storagePath = await UploadChunkToStorageAsync(chunkFileName, stream);

		// 记录分片到数据库
		var chunk = new FileUploadChunk
		{
			UploadId = uploadId,
			ChunkIndex = chunkIndex,
			ChunkHash = chunkHash,
			Size = stream.Length,
			StoragePath = storagePath
		};

		_dbContext.UploadChunks.Add(chunk);

		// 更新已上传分片数
		session.UploadedChunks++;
		session.UpdatedAtUtc = DateTime.UtcNow;

		await _dbContext.SaveChangesAsync();

		// 记录到 Redis Set
		await AddChunkToRedisAsync(uploadId, chunkIndex);

		_logger.LogInformation("Uploaded chunk {ChunkIndex}/{TotalChunks} for upload {UploadId}",
			chunkIndex, session.TotalChunks, uploadId);
	}

	/// <inheritdoc />
	public async Task CompleteUploadAsync(Guid uploadId)
	{
		var session = await _dbContext.UploadSessions
			.FirstOrDefaultAsync(s => s.Id == uploadId);

		if (session is null)
			throw new BusinessException("上传会话不存在");

		if (session.Status != UploadStatus.Uploading)
			throw new BusinessException($"上传会话状态为 {session.Status}，无法完成");

		// 从 Redis 获取已上传分片数量
		var uploadedIndexes = await GetUploadedChunkIndexesFromRedisAsync(uploadId);
		var uploadedCount = uploadedIndexes.Count > 0 ? uploadedIndexes.Count : session.UploadedChunks;

		if (uploadedCount < session.TotalChunks)
			throw new BusinessException($"还有 {session.TotalChunks - uploadedCount} 个分片未上传");

		// 更新状态为合并中，由后台服务执行实际合并
		session.Status = UploadStatus.Merging;
		session.UpdatedAtUtc = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync();

		// 清理 Redis 键
		await CleanupRedisKeysAsync(uploadId);

		_logger.LogInformation("Upload {UploadId} marked as Merging, background service will process it", uploadId);
	}

	/// <inheritdoc />
	public async Task<CheckByHashResultDto> CheckByHashAsync(CheckByHashRequestDto request)
	{
		if (string.IsNullOrWhiteSpace(request.FileHash))
			throw new BusinessException("文件 Hash 不能为空");

		if (request.FileSize <= 0)
			throw new BusinessException("文件大小必须大于 0");

		// 1. 查找已完成的分片上传会话
		var existingSession = await _dbContext.UploadSessions
			.FirstOrDefaultAsync(s =>
				s.FileHash == request.FileHash &&
				s.FileSize == request.FileSize &&
				s.Status == UploadStatus.Completed);

		if (existingSession?.FileId is not null)
		{
			var file = await _dbContext.Files.FindAsync(existingSession.FileId);
			if (file is not null)
			{
				return new CheckByHashResultDto
				{
					Exists = true,
					FileId = file.Id,
					FileName = file.FileName,
					FileSize = file.FileSize
				};
			}
		}

		// 2. 直接查找 FileEntity（覆盖普通上传的文件）
		var existingFile = await _dbContext.Files
			.FirstOrDefaultAsync(f => f.FileHash == request.FileHash && f.FileSize == request.FileSize);

		if (existingFile is not null)
		{
			return new CheckByHashResultDto
			{
				Exists = true,
				FileId = existingFile.Id,
				FileName = existingFile.FileName,
				FileSize = existingFile.FileSize
			};
		}

		return new CheckByHashResultDto { Exists = false };
	}

	/// <inheritdoc />
	public async Task CancelUploadAsync(Guid uploadId)
	{
		var session = await _dbContext.UploadSessions
			.Include(s => s.Chunks)
			.FirstOrDefaultAsync(s => s.Id == uploadId);

		if (session is null)
			throw new BusinessException("上传会话不存在");

		if (session.Status == UploadStatus.Completed)
			throw new BusinessException("已完成的上传无法取消");

		// 删除分片文件
		foreach (var chunk in session.Chunks)
		{
			try
			{
				await _fileStorageService.DeleteAsync(chunk.StoragePath);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Failed to delete chunk file {StoragePath}", chunk.StoragePath);
			}
		}

		// 删除数据库记录
		_dbContext.UploadChunks.RemoveRange(session.Chunks);
		_dbContext.UploadSessions.Remove(session);
		await _dbContext.SaveChangesAsync();

		// 清理 Redis 键
		await CleanupRedisKeysAsync(uploadId);

		_logger.LogInformation("Cancelled upload {UploadId}", uploadId);
	}

	#region Redis 操作

	/// <summary>
	/// 获取 Redis 键：file:upload:{uploadId}:chunks
	/// </summary>
	private static string GetChunksRedisKey(Guid uploadId)
	{
		return $"file:upload:{uploadId}:chunks";
	}

	/// <summary>
	/// 从 Redis 获取已上传分片序号列表。
	/// </summary>
	private async Task<List<int>> GetUploadedChunkIndexesFromRedisAsync(Guid uploadId)
	{
		var redisKey = GetChunksRedisKey(uploadId);
		var exists = await _redisCacheManager.ExistsAsync(redisKey);

		if (!exists)
			return [];

		var db = _redisCacheManager.GetDatabase();
		var members = await db.SetMembersAsync(redisKey);

		return members
			.Select(m => int.TryParse(m.ToString(), out var index) ? index : -1)
			.Where(i => i >= 0)
			.OrderBy(i => i)
			.ToList();
	}

	/// <summary>
	/// 添加分片序号到 Redis Set。
	/// </summary>
	private async Task AddChunkToRedisAsync(Guid uploadId, int chunkIndex)
	{
		var redisKey = GetChunksRedisKey(uploadId);
		var db = _redisCacheManager.GetDatabase();
		await db.SetAddAsync(redisKey, chunkIndex.ToString());

		// 设置过期时间（10 分钟）
		await db.KeyExpireAsync(redisKey, TimeSpan.FromMinutes(10));
	}

	/// <summary>
	/// 同步数据库中的分片记录到 Redis。
	/// </summary>
	private async Task SyncChunksToRedisAsync(Guid uploadId, List<int> chunkIndexes)
	{
		if (chunkIndexes.Count == 0)
			return;

		var redisKey = GetChunksRedisKey(uploadId);
		var db = _redisCacheManager.GetDatabase();

		foreach (var index in chunkIndexes)
		{
			await db.SetAddAsync(redisKey, index.ToString());
		}

		await db.KeyExpireAsync(redisKey, TimeSpan.FromMinutes(10));
	}

	/// <summary>
	/// 清理 Redis 键。
	/// </summary>
	private async Task CleanupRedisKeysAsync(Guid uploadId)
	{
		var redisKey = GetChunksRedisKey(uploadId);
		await _redisCacheManager.RemoveAsync(redisKey);
		await _redisCacheManager.RemoveAsync($"{redisKey}:init");
	}

	#endregion

	/// <summary>
	/// 上传分片到存储。
	/// </summary>
	private async Task<string> UploadChunkToStorageAsync(string chunkFileName, Stream stream)
	{
		return await _fileStorageService.UploadAsync(stream, chunkFileName, "application/octet-stream");
	}

	/// <summary>
	/// 合并分片为完整文件（由后台合并服务调用）。
	/// </summary>
	public async Task MergeChunksAsync(FileUploadSession session)
	{
		var orderedChunks = session.Chunks
			.OrderBy(c => c.ChunkIndex)
			.ToList();

		string filePath;
		var extension = Path.GetExtension(session.FileName);
		var contentType = GetContentType(extension);

		// 秒传检查：已有相同 hash + size 的文件，复用其存储路径
		var existingFile = await _dbContext.Files
			.AsNoTracking()
			.FirstOrDefaultAsync(f => f.FileHash == session.FileHash && f.FileSize == session.FileSize);

		if (existingFile is not null)
		{
			filePath = existingFile.FilePath;
		}
		else
		{
			// 用 ConcatenatedReadStream 串联所有分片流，避免将整个文件加载到内存
			var chunkStreams = new List<Stream>(orderedChunks.Count);
			foreach (var chunk in orderedChunks)
			{
				chunkStreams.Add(await _fileStorageService.GetStreamAsync(chunk.StoragePath));
			}

			await using (var mergedStream = new ConcatenatedReadStream(chunkStreams))
			{
				// 流式上传合并后的文件
				filePath = await _fileStorageService.UploadAsync(mergedStream, session.FileName, contentType);
			}
		}

		// 创建 FileEntity 记录
		var fileEntity = new FileEntity(
			session.FileName,
			filePath,
			session.FileSize,
			contentType,
			extension,
			session.AccessLevel,
			session.CreatorName,
			session.FileHash);

		_dbContext.Files.Add(fileEntity);

		// 更新上传会话
		session.Status = UploadStatus.Completed;
		session.FileId = fileEntity.Id;
		session.UpdatedAtUtc = DateTime.UtcNow;

		await _dbContext.SaveChangesAsync();

		// 清理分片文件（异步）
		_ = Task.Run(async () =>
		{
			foreach (var chunk in orderedChunks)
			{
				try
				{
					await _fileStorageService.DeleteAsync(chunk.StoragePath);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to delete chunk file {StoragePath}", chunk.StoragePath);
				}
			}
		});

		_logger.LogInformation("Merged {ChunkCount} chunks for upload {UploadId}, file: {FilePath}",
			orderedChunks.Count, session.Id, filePath);
	}

	/// <summary>
	/// 根据扩展名获取 Content Type。
	/// </summary>
	private static string GetContentType(string extension)
	{
		return extension.ToLowerInvariant() switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".gif" => "image/gif",
			".pdf" => "application/pdf",
			".mp4" => "video/mp4",
			".mp3" => "audio/mpeg",
			".zip" => "application/zip",
			_ => "application/octet-stream"
		};
	}
}
