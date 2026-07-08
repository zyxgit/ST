using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Domain.Enums;
using ST.MS.FileUpload.Domain.Services;
using ST.MS.FileUpload.Infra.DbContext;
using ST.Infra.Redis.Cache;

namespace ST.MS.FileUpload.Application.Services;

/// <summary>
/// 分片清理后台服务。
/// 定期扫描过期（Uploading 超时）和失败（Failed）的上传会话，
/// 删除分片文件、数据库记录并清理 Redis 键。
/// </summary>
public sealed class MultipartCleanupService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IOptions<MultipartCleanupOptions> _options;
	private readonly ILogger<MultipartCleanupService> _logger;

	public MultipartCleanupService(
		IServiceScopeFactory scopeFactory,
		IOptions<MultipartCleanupOptions> options,
		ILogger<MultipartCleanupService> logger)
	{
		_scopeFactory = scopeFactory;
		_options = options;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!_options.Value.Enabled)
		{
			_logger.LogInformation("Multipart cleanup service is disabled.");
			return;
		}

		var interval = TimeSpan.FromSeconds(_options.Value.PollingIntervalSeconds);
		_logger.LogInformation(
			"Multipart cleanup service started. Interval={Interval} BatchSize={BatchSize} FailedRetention={Retention}s",
			interval, _options.Value.BatchSize, _options.Value.FailedRetentionSeconds);

		using var timer = new PeriodicTimer(interval);

		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			try
			{
				await CleanupExpiredSessionsAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred during multipart cleanup processing.");
			}
		}
	}

	/// <summary>
	/// 扫描并清理过期和失败的上传会话。
	/// </summary>
	private async Task CleanupExpiredSessionsAsync(CancellationToken ct)
	{
		using var scope = _scopeFactory.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<FileUploadDbContext>();
		var fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
		var redisCacheManager = scope.ServiceProvider.GetRequiredService<IRedisCacheManager>();

		var batchSize = _options.Value.BatchSize;
		var now = DateTime.UtcNow;
		var failedThreshold = now.AddSeconds(-_options.Value.FailedRetentionSeconds);

		// 1. 过期的 Uploading 会话（超过 ExpiresAtUtc）
		var expiredSessions = await dbContext.UploadSessions
			.Include(s => s.Chunks)
			.Where(s => s.Status == UploadStatus.Uploading && s.ExpiresAtUtc < now)
			.OrderBy(s => s.ExpiresAtUtc)
			.Take(batchSize)
			.ToListAsync(ct);

		// 2. 失败的会话（超过保留时间）
		var failedSessions = await dbContext.UploadSessions
			.Include(s => s.Chunks)
			.Where(s => s.Status == UploadStatus.Failed && s.UpdatedAtUtc < failedThreshold)
			.OrderBy(s => s.UpdatedAtUtc)
			.Take(batchSize)
			.ToListAsync(ct);

		var allSessions = expiredSessions.Concat(failedSessions).ToList();

		if (allSessions.Count == 0)
		{
			_logger.LogDebug("No expired or failed sessions to clean up.");
			return;
		}

		_logger.LogInformation("Found {Count} sessions to clean up ({Expired} expired, {Failed} failed).",
			allSessions.Count, expiredSessions.Count, failedSessions.Count);

		var cleanedCount = 0;
		var chunkDeleteFailed = 0;

		foreach (var session in allSessions)
		{
			ct.ThrowIfCancellationRequested();

			try
			{
				// 删除分片文件
				foreach (var chunk in session.Chunks)
				{
					try
					{
						await fileStorageService.DeleteAsync(chunk.StoragePath);
					}
					catch (Exception ex)
					{
						chunkDeleteFailed++;
						_logger.LogWarning(ex, "Failed to delete chunk file {StoragePath} for session {UploadId}.",
							chunk.StoragePath, session.Id);
					}
				}

				// 删除数据库记录
				dbContext.UploadChunks.RemoveRange(session.Chunks);

				if (session.Status == UploadStatus.Uploading)
				{
					// 过期会话标记为 Expired
					session.Status = UploadStatus.Expired;
					session.UpdatedAtUtc = now;
					session.ErrorMessage = $"自动清理于 {now:O}";
				}
				else
				{
					// Failed 会话直接删除
					dbContext.UploadSessions.Remove(session);
				}

				// 清理 Redis 键
				await CleanupRedisKeysAsync(redisCacheManager, session.Id);

				cleanedCount++;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to clean up session {UploadId}.", session.Id);
			}
		}

		await dbContext.SaveChangesAsync(ct);

		_logger.LogInformation(
			"Multipart cleanup batch completed. Cleaned={Cleaned} ChunkDeleteFailed={ChunkDeleteFailed}",
			cleanedCount, chunkDeleteFailed);
	}

	/// <summary>
	/// 清理 Redis 中的分片记录键。
	/// </summary>
	private static async Task CleanupRedisKeysAsync(IRedisCacheManager redisCacheManager, Guid uploadId)
	{
		var redisKey = $"file:upload:{uploadId}:chunks";
		await redisCacheManager.RemoveAsync(redisKey);
		await redisCacheManager.RemoveAsync($"{redisKey}:init");
	}
}
