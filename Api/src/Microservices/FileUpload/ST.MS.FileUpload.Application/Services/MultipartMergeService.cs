using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Application.IServices;
using ST.MS.FileUpload.Domain.Enums;
using ST.MS.FileUpload.Infra.DbContext;

namespace ST.MS.FileUpload.Application.Services;

/// <summary>
/// 分片合并后台服务。
/// 定期扫描 Merging 状态的上传会话，执行分片合并。
/// 合并失败时支持重试，超过最大重试次数标记为 Failed。
/// </summary>
public sealed class MultipartMergeService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IOptions<MultipartMergeOptions> _options;
	private readonly ILogger<MultipartMergeService> _logger;

	public MultipartMergeService(
		IServiceScopeFactory scopeFactory,
		IOptions<MultipartMergeOptions> options,
		ILogger<MultipartMergeService> logger)
	{
		_scopeFactory = scopeFactory;
		_options = options;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!_options.Value.Enabled)
		{
			_logger.LogInformation("Multipart merge service is disabled.");
			return;
		}

		var interval = TimeSpan.FromSeconds(_options.Value.PollingIntervalSeconds);
		_logger.LogInformation(
			"Multipart merge service started. Interval={Interval} BatchSize={BatchSize} MaxRetry={MaxRetry}",
			interval, _options.Value.BatchSize, _options.Value.MaxRetryCount);

		using var timer = new PeriodicTimer(interval);

		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			try
			{
				await ProcessMergingSessionsAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred during multipart merge processing.");
			}
		}
	}

	/// <summary>
	/// 扫描并处理 Merging 状态的上传会话。
	/// </summary>
	private async Task ProcessMergingSessionsAsync(CancellationToken ct)
	{
		using var scope = _scopeFactory.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<FileUploadDbContext>();
		var multipartUploadService = scope.ServiceProvider.GetRequiredService<IMultipartUploadService>();

		var batchSize = _options.Value.BatchSize;
		var maxRetry = _options.Value.MaxRetryCount;

		// 查询 Merging 状态的会话，按创建时间排序
		var mergingSessions = await dbContext.UploadSessions
			.Include(s => s.Chunks)
			.Where(s => s.Status == UploadStatus.Merging)
			.OrderBy(s => s.CreatedAtUtc)
			.Take(batchSize)
			.ToListAsync(ct);

		if (mergingSessions.Count == 0)
		{
			_logger.LogDebug("No merging sessions found.");
			return;
		}

		_logger.LogInformation("Found {Count} merging sessions to process.", mergingSessions.Count);

		var successCount = 0;
		var failCount = 0;

		foreach (var session in mergingSessions)
		{
			try
			{
				await multipartUploadService.MergeChunksAsync(session);
				successCount++;

				_logger.LogInformation(
					"Merged chunks for upload {UploadId}. File={FileName} Chunks={ChunkCount}",
					session.Id, session.FileName, session.Chunks.Count);
			}
			catch (Exception ex)
			{
				failCount++;

				// 更新重试计数和错误信息
				session.ErrorMessage = ex.Message;
				session.UpdatedAtUtc = DateTime.UtcNow;

				// 检查是否超过最大重试次数
				// 使用 ErrorMessage 中的重试标记来跟踪（简化方案，避免修改实体结构）
				var retryCount = GetRetryCount(session.ErrorMessage);
				if (retryCount >= maxRetry)
				{
					session.Status = UploadStatus.Failed;
					session.ErrorMessage = $"合并失败（已重试 {retryCount} 次）: {ex.Message}";

					_logger.LogError(ex,
						"Failed to merge chunks for upload {UploadId} after {RetryCount} retries, marking as Failed.",
						session.Id, retryCount);
				}
				else
				{
					// 保留错误信息供下次重试参考
					session.ErrorMessage = $"[retry:{retryCount + 1}] {ex.Message}";

					_logger.LogWarning(ex,
						"Failed to merge chunks for upload {UploadId}, will retry. Attempt={Attempt}/{MaxRetry}",
						session.Id, retryCount + 1, maxRetry);
				}

				await dbContext.SaveChangesAsync(ct);
			}
		}

		_logger.LogInformation(
			"Multipart merge batch completed. Total={Total} Success={Success} Failed={Failed}",
			mergingSessions.Count, successCount, failCount);
	}

	/// <summary>
	/// 从错误信息中解析重试次数。
	/// 格式：[retry:N] error message
	/// </summary>
	private static int GetRetryCount(string? errorMessage)
	{
		if (string.IsNullOrEmpty(errorMessage))
			return 0;

		var match = System.Text.RegularExpressions.Regex.Match(errorMessage, @"^\[retry:(\d+)\]");
		return match.Success && int.TryParse(match.Groups[1].Value, out var count) ? count : 0;
	}
}
