using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// Outbox Publisher 后台服务。
/// 周期性扫描可重试的 Outbox 消息并通过 IOutboxPublisher 投递到消息代理。
/// 发送成功标记 Sent，失败按指数退避设置下次重试时间，超过最大重试次数不再自动重试。
/// </summary>
public sealed class OutboxPublisherHostedService : BackgroundService
{
	private readonly OutboxPublisherOptions _options;
	private readonly IOutboxPublisher _publisher;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<OutboxPublisherHostedService> _logger;

	public OutboxPublisherHostedService(
		OutboxPublisherOptions options,
		IOutboxPublisher publisher,
		IServiceScopeFactory scopeFactory,
		ILogger<OutboxPublisherHostedService> logger)
	{
		_options = options;
		_publisher = publisher;
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation(
			"Outbox Publisher started. PollingInterval={Interval}s BatchSize={BatchSize} MaxRetry={MaxRetry} Exchange={Exchange}",
			_options.PollingIntervalSeconds, _options.BatchSize, _options.MaxRetryCount, _options.ExchangeName);

		using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollingIntervalSeconds));

		while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
		{
			try
			{
				await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Outbox Publisher batch processing error.");
			}
		}

		_logger.LogInformation("Outbox Publisher stopped.");
	}

	private async Task ProcessBatchAsync(CancellationToken ct)
	{
		using var scope = _scopeFactory.CreateScope();
		var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

		var messages = await store.GetRetryableAsync(_options.BatchSize, ct).ConfigureAwait(false);

		if (messages.Count == 0)
		{
			return;
		}

		_logger.LogDebug("Outbox Publisher processing {Count} messages.", messages.Count);

		foreach (var message in messages)
		{
			ct.ThrowIfCancellationRequested();

			try
			{
				var sw = System.Diagnostics.Stopwatch.StartNew();
				await _publisher.PublishAsync(message, ct).ConfigureAwait(false);
				sw.Stop();
				OutboxMetrics.Published.Add(1);
				OutboxMetrics.PublishDurationMs.Record(sw.Elapsed.TotalMilliseconds);

				await store.MarkAsSentAsync(message.Id, ct).ConfigureAwait(false);

				_logger.LogDebug(
					"Outbox message published. Id={MessageId} EventType={EventType} AggregateId={AggregateId}",
					message.Id, message.EventType, message.AggregateId);
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				await HandlePublishFailureAsync(store, message, ex, ct).ConfigureAwait(false);
			}

			// 每条消息处理后 SaveChanges，避免一条失败影响整批
			await store.SaveChangesAsync(ct).ConfigureAwait(false);
		}
	}

	private async Task HandlePublishFailureAsync(
		IOutboxStore store, OutboxMessage message, Exception ex, CancellationToken ct)
	{
		var newRetryCount = message.RetryCount + 1;

		if (newRetryCount >= _options.MaxRetryCount)
		{
			// 超过最大重试次数，标记为 Failed 不再自动重试
			OutboxMetrics.Failed.Add(1);
			var nextRetry = DateTime.UtcNow.AddYears(100); // 永不重试
			await store.MarkAsFailedAsync(message.Id, $"Max retry count exceeded: {ex.Message}", nextRetry, ct)
				.ConfigureAwait(false);

			_logger.LogError(ex,
				"Outbox message exceeded max retry count and will not be retried. Id={MessageId} EventType={EventType} RetryCount={RetryCount}",
				message.Id, message.EventType, newRetryCount);
		}
		else
		{
			// 指数退避: BaseRetryDelay * 2^retryCount
			OutboxMetrics.Retried.Add(1);
			var delaySeconds = _options.BaseRetryDelaySeconds * (1 << message.RetryCount);
			var nextRetry = DateTime.UtcNow.AddSeconds(delaySeconds);

			await store.MarkAsFailedAsync(message.Id, ex.Message, nextRetry, ct)
				.ConfigureAwait(false);

			_logger.LogWarning(ex,
				"Outbox message publish failed, will retry. Id={MessageId} EventType={EventType} RetryCount={RetryCount} NextRetry={NextRetry}",
				message.Id, message.EventType, newRetryCount, nextRetry);
		}
	}
}
