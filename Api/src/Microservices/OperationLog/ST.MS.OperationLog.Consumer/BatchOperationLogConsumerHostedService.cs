using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ST.Infra.EventBus.OperationLog;
using ST.MS.OperationLog.Infra.DbContext;
using ST.Shared.OperationLog;

namespace ST.MS.OperationLog.Consumer;

/// <summary>
/// 批量消费操作日志的后台服务。
/// 支持：
/// - 配置化 prefetch、批量大小、刷新间隔
/// - 内存缓冲队列，每 N 条或每 T 秒批量写库
/// - 批量失败时降级为单条写入定位毒丸消息
/// </summary>
public sealed class BatchOperationLogConsumerHostedService : BackgroundService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private readonly RabbitMqOperationLogOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<BatchOperationLogConsumerHostedService> _logger;

	private IConnection? _connection;
	private IChannel? _channel;
	private string? _consumerTag;

	// 内存缓冲区
	private readonly List<BufferedOperationLogEntry> _buffer = [];
	private readonly object _bufferLock = new();
	private DateTime _lastFlushTime = DateTime.UtcNow;

	// 统计信息
	private long _totalReceived;
	private long _totalBatchWritten;
	private long _totalSingleWritten;
	private long _totalFailed;

	public BatchOperationLogConsumerHostedService(
		RabbitMqOperationLogOptions options,
		IServiceScopeFactory scopeFactory,
		ILogger<BatchOperationLogConsumerHostedService> logger)
	{
		_options = options;
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await ConnectAndStartConsumeAsync(stoppingToken).ConfigureAwait(false);

		// 启动定时刷新任务
		_ = Task.Run(() => PeriodicFlushAsync(stoppingToken), stoppingToken);

		try
		{
			await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		{
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		_logger.LogInformation("Stopping batch consumer. Flushing remaining buffer...");

		// 停止时刷新缓冲区
		await FlushBufferAsync("shutdown").ConfigureAwait(false);

		// 输出统计信息
		_logger.LogInformation(
			"Batch consumer stats: Received={Received}, BatchWritten={BatchWritten}, SingleWritten={SingleWritten}, Failed={Failed}",
			_totalReceived, _totalBatchWritten, _totalSingleWritten, _totalFailed);

		try
		{
			if (!string.IsNullOrWhiteSpace(_consumerTag) && _channel is { IsOpen: true })
			{
				await _channel.BasicCancelAsync(_consumerTag, cancellationToken: cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Error cancelling consumer.");
		}

		try { _channel?.Dispose(); } catch { }
		try { _connection?.Dispose(); } catch { }

		await base.StopAsync(cancellationToken).ConfigureAwait(false);
	}

	private async Task ConnectAndStartConsumeAsync(CancellationToken cancellationToken)
	{
		var factory = new ConnectionFactory
		{
			HostName = _options.HostName,
			Port = _options.Port,
			UserName = _options.UserName,
			Password = _options.Password,
			VirtualHost = _options.VirtualHost,
			AutomaticRecoveryEnabled = true,
			NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
		};

		_connection = await factory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
		_channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

		await _channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Direct, durable: _options.Durable, autoDelete: _options.AutoDelete, cancellationToken: cancellationToken).ConfigureAwait(false);

		var queueName = string.IsNullOrWhiteSpace(_options.QueueName) ? "st.operationlog.consumer" : _options.QueueName;
		await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken).ConfigureAwait(false);
		await _channel.QueueBindAsync(queue: queueName, exchange: _options.ExchangeName, routingKey: _options.RoutingKey, cancellationToken: cancellationToken).ConfigureAwait(false);

		await _channel.BasicQosAsync(0, _options.PrefetchCount, global: false, cancellationToken: cancellationToken).ConfigureAwait(false);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += OnReceivedAsync;

		_consumerTag = await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken).ConfigureAwait(false);

		_logger.LogInformation(
			"Batch consumer started. Exchange={Exchange} Queue={Queue} Prefetch={Prefetch} BatchSize={BatchSize} FlushInterval={FlushInterval}s",
			_options.ExchangeName, queueName, _options.PrefetchCount, _options.BatchSize, _options.FlushIntervalSeconds);
	}

	/// <summary>
	/// 接收消息并加入缓冲区。
	/// </summary>
	private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
	{
		var bodyText = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
		OperationLogEntry? entry;

		try
		{
			entry = JsonSerializer.Deserialize<OperationLogEntry>(bodyText, JsonOptions);
		}
		catch (JsonException ex)
		{
			_logger.LogError(ex, "Failed to deserialize message. Acknowledging and skipping.");
			await _channel!.BasicAckAsync(eventArgs.DeliveryTag, false).ConfigureAwait(false);
			return;
		}

		if (entry is null)
		{
			await _channel!.BasicAckAsync(eventArgs.DeliveryTag, false).ConfigureAwait(false);
			return;
		}

		// 创建缓冲区条目
		var bufferedEntry = new BufferedOperationLogEntry
		{
			Entry = entry,
			DeliveryTag = eventArgs.DeliveryTag
		};

		bool shouldFlush;
		lock (_bufferLock)
		{
			_buffer.Add(bufferedEntry);
			_totalReceived++;
			shouldFlush = _buffer.Count >= _options.BatchSize;
		}

		OperationLogMetrics.MessagesReceived.Add(1);

		// 达到批量大小，触发写库
		if (shouldFlush)
		{
			await FlushBufferAsync("batch-full").ConfigureAwait(false);
		}
	}

	/// <summary>
	/// 定时刷新缓冲区。
	/// </summary>
	private async Task PeriodicFlushAsync(CancellationToken stoppingToken)
	{
		var interval = TimeSpan.FromSeconds(_options.FlushIntervalSeconds);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(interval, stoppingToken).ConfigureAwait(false);

				bool hasData;
				lock (_bufferLock)
				{
					hasData = _buffer.Count > 0;
				}

				if (hasData)
				{
					await FlushBufferAsync("timer").ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in periodic flush.");
			}
		}
	}

	/// <summary>
	/// 刷新缓冲区，批量写库。
	/// </summary>
	private async Task FlushBufferAsync(string reason)
	{
		List<BufferedOperationLogEntry> batch;

		lock (_bufferLock)
		{
			if (_buffer.Count == 0)
				return;

			batch = [.. _buffer];
			_buffer.Clear();
			_lastFlushTime = DateTime.UtcNow;
		}

		_logger.LogDebug("Flushing {Count} logs. Reason: {Reason}", batch.Count, reason);

		var sw = System.Diagnostics.Stopwatch.StartNew();

		try
		{
			await BatchWriteToDatabaseAsync(batch).ConfigureAwait(false);
			_totalBatchWritten += batch.Count;

			sw.Stop();
			OperationLogMetrics.BatchWriteSuccess.Add(batch.Count);
			OperationLogMetrics.BatchSize.Record(batch.Count);
			OperationLogMetrics.FlushDurationMs.Record(sw.Elapsed.TotalMilliseconds);

			// 批量 ack
			foreach (var entry in batch)
			{
				try
				{
					await _channel!.BasicAckAsync(entry.DeliveryTag, false).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to ack message after batch write.");
				}
			}
		}
		catch (Exception batchEx)
		{
			sw.Stop();
			OperationLogMetrics.FlushDurationMs.Record(sw.Elapsed.TotalMilliseconds);
			_logger.LogError(batchEx, "Batch write failed for {Count} logs.", batch.Count);

			if (_options.FallbackToSingleOnBatchFailure)
			{
				// 降级为单条写入
				await FallbackToSingleWriteAsync(batch).ConfigureAwait(false);
			}
			else
			{
				// 不降级，所有消息 nack 重试
				foreach (var entry in batch)
				{
					try
					{
						await _channel!.BasicNackAsync(entry.DeliveryTag, false, _options.RequeueOnError).ConfigureAwait(false);
						_totalFailed++;
						OperationLogMetrics.WriteFailed.Add(1);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to nack message after batch failure.");
					}
				}
			}
		}
	}

	/// <summary>
	/// 批量写入数据库。
	/// </summary>
	private async Task BatchWriteToDatabaseAsync(List<BufferedOperationLogEntry> batch)
	{
		using var scope = _scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OperationLogDbContext>();

		var entities = batch.Select(buffered => new ST.Infra.EntityFramework.OperationLogs.OperationLog
		{
			CreatedAtUtc = buffered.Entry.OccurredOnUtc.UtcDateTime,
			ServiceName = buffered.Entry.ServiceName,
			TraceId = buffered.Entry.TraceId,
			SpanId = buffered.Entry.SpanId,
			UserId = buffered.Entry.UserId,
			TenantId = buffered.Entry.TenantId,
			UserName = buffered.Entry.UserName,
			OperationName = buffered.Entry.OperationName,
			Path = buffered.Entry.Path,
			Method = buffered.Entry.Method,
			Ip = buffered.Entry.Ip,
			StatusCode = buffered.Entry.StatusCode,
			Success = buffered.Entry.Success,
			DurationMs = buffered.Entry.DurationMs,
			RequestJson = buffered.Entry.RequestJson,
			ResponseJson = buffered.Entry.ResponseJson,
			ExceptionType = buffered.Entry.ExceptionType,
			ExceptionMessage = buffered.Entry.ExceptionMessage,
			ExceptionStackTrace = buffered.Entry.ExceptionStackTrace
		}).ToList();

		db.OperationLogs.AddRange(entities);
		await db.SaveChangesAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// 批量写库失败时，降级为单条写入。
	/// </summary>
	private async Task FallbackToSingleWriteAsync(List<BufferedOperationLogEntry> batch)
	{
		_logger.LogWarning("Falling back to single write for {Count} logs.", batch.Count);

		foreach (var buffered in batch)
		{
			try
			{
				using var scope = _scopeFactory.CreateScope();
				var db = scope.ServiceProvider.GetRequiredService<OperationLogDbContext>();

				db.OperationLogs.Add(new ST.Infra.EntityFramework.OperationLogs.OperationLog
				{
					CreatedAtUtc = buffered.Entry.OccurredOnUtc.UtcDateTime,
					ServiceName = buffered.Entry.ServiceName,
					TraceId = buffered.Entry.TraceId,
					SpanId = buffered.Entry.SpanId,
					UserId = buffered.Entry.UserId,
					TenantId = buffered.Entry.TenantId,
					UserName = buffered.Entry.UserName,
					OperationName = buffered.Entry.OperationName,
					Path = buffered.Entry.Path,
					Method = buffered.Entry.Method,
					Ip = buffered.Entry.Ip,
					StatusCode = buffered.Entry.StatusCode,
					Success = buffered.Entry.Success,
					DurationMs = buffered.Entry.DurationMs,
					RequestJson = buffered.Entry.RequestJson,
					ResponseJson = buffered.Entry.ResponseJson,
					ExceptionType = buffered.Entry.ExceptionType,
					ExceptionMessage = buffered.Entry.ExceptionMessage,
					ExceptionStackTrace = buffered.Entry.ExceptionStackTrace
				});

				await db.SaveChangesAsync().ConfigureAwait(false);
				await _channel!.BasicAckAsync(buffered.DeliveryTag, false).ConfigureAwait(false);
				_totalSingleWritten++;
				OperationLogMetrics.SingleWriteSuccess.Add(1);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Single write also failed. Sending to dead letter table.");
				await SendToDeadLetterAsync(buffered, ex);
			}
		}
	}

	/// <summary>
	/// 将失败消息发送到死信表。
	/// </summary>
	private async Task SendToDeadLetterAsync(BufferedOperationLogEntry buffered, Exception ex)
	{
		try
		{
			using var scope = _scopeFactory.CreateScope();
			var deadLetterService = scope.ServiceProvider.GetRequiredService<DeadLetterService>();

			var originalMessage = JsonSerializer.Serialize(buffered.Entry, JsonOptions);

			await deadLetterService.SendToDeadLetterAsync(
				originalMessage: originalMessage,
				queueName: _options.QueueName,
				exchangeName: _options.ExchangeName,
				routingKey: _options.RoutingKey,
				errorMessage: ex.Message,
				errorStackTrace: ex.StackTrace,
				retryCount: 0,
				maxRetryCount: _options.MaxRetryCount,
				messageCreatedAtUtc: buffered.Entry.OccurredOnUtc.UtcDateTime);

			// ACK 消息（已保存到死信表，不再重试）
			await _channel!.BasicAckAsync(buffered.DeliveryTag, false).ConfigureAwait(false);
			_totalFailed++;
			OperationLogMetrics.WriteFailed.Add(1);
			OperationLogMetrics.DeadLetterWritten.Add(1);
		}
		catch (Exception deadLetterEx)
		{
			_logger.LogError(deadLetterEx, "Failed to send message to dead letter table.");
			// 最后手段：NACK 消息
			try
			{
				await _channel!.BasicNackAsync(buffered.DeliveryTag, false, false).ConfigureAwait(false);
			}
			catch (Exception nackEx)
			{
				_logger.LogWarning(nackEx, "Failed to nack message after dead letter failure.");
			}
		}
	}
}
