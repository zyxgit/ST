using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.MS.OperationLog.Infra.Archive;

namespace ST.MS.OperationLog.Consumer;

/// <summary>
/// 操作日志归档后台任务。
/// 定期将历史日志从 PostgreSQL 归档到文件系统或对象存储。
/// </summary>
public sealed class OperationLogArchiveJob : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<OperationLogArchiveJob> _logger;
	private readonly OperationLogArchiveOptions _options;

	public OperationLogArchiveJob(
		IServiceScopeFactory scopeFactory,
		IOptions<OperationLogArchiveOptions> options,
		ILogger<OperationLogArchiveJob> logger)
	{
		_scopeFactory = scopeFactory;
		_options = options.Value;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!_options.Enabled)
		{
			_logger.LogInformation("OperationLog archive job is disabled.");
			return;
		}

		_logger.LogInformation(
			"OperationLog archive job started. ArchiveAfterDays={Days} Interval={Interval}h StorageType={StorageType}",
			_options.ArchiveAfterDays, _options.ExecutionIntervalHours, _options.StorageType);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await RunArchiveAsync(stoppingToken);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Archive job execution failed.");
			}

			// 等待下一次执行
			var delay = TimeSpan.FromHours(_options.ExecutionIntervalHours);
			_logger.LogInformation("Next archive execution in {Delay}", delay);
			await Task.Delay(delay, stoppingToken);
		}
	}

	private async Task RunArchiveAsync(CancellationToken cancellation)
	{
		_logger.LogInformation("Starting archive batch...");

		using var scope = _scopeFactory.CreateScope();
		var archiveService = scope.ServiceProvider.GetRequiredService<IArchiveService>();

		var totalArchived = 0;
		ArchiveResult? lastResult = null;

		// 循环归档，直到没有更多数据
		while (!cancellation.IsCancellationRequested)
		{
			var result = await archiveService.ArchiveAsync(cancellation);
			lastResult = result;

			if (!result.Success)
			{
				_logger.LogError("Archive batch failed: {Error}", result.ErrorMessage);
				OperationLogMetrics.ArchiveFailed.Add(1);
				break;
			}

			if (result.ArchivedCount == 0)
			{
				break;
			}

			totalArchived += result.ArchivedCount;
			OperationLogMetrics.ArchiveCount.Add(result.ArchivedCount);
			_logger.LogInformation("Archived {Count} logs. Total: {Total}", result.ArchivedCount, totalArchived);

			// 避免长时间占用资源
			await Task.Delay(1000, cancellation);
		}

		_logger.LogInformation("Archive batch completed. Total archived: {Total}", totalArchived);
	}
}
