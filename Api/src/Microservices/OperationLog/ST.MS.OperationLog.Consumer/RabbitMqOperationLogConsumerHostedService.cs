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

public sealed class RabbitMqOperationLogConsumerHostedService : BackgroundService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private readonly RabbitMqOperationLogOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<RabbitMqOperationLogConsumerHostedService> _logger;

	private IConnection? _connection;
	private IChannel? _channel;
	private string? _consumerTag;

	public RabbitMqOperationLogConsumerHostedService(
		RabbitMqOperationLogOptions options,
		IServiceScopeFactory scopeFactory,
		ILogger<RabbitMqOperationLogConsumerHostedService> logger)
	{
		_options = options;
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await ConnectAndStartConsumeAsync(stoppingToken).ConfigureAwait(false);

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

		_logger.LogInformation("OperationLog consumer started. Exchange={Exchange} Queue={Queue} RoutingKey={RoutingKey}",
			_options.ExchangeName, queueName, _options.RoutingKey);
	}

	private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
	{
		var bodyText = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
		try
		{
			var entry = JsonSerializer.Deserialize<OperationLogEntry>(bodyText, JsonOptions);
			if (entry is null)
			{
				if (_channel is not null)
				{
					await _channel.BasicAckAsync(eventArgs.DeliveryTag, false).ConfigureAwait(false);
				}

				return;
			}

			using var scope = _scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<OperationLogDbContext>();

			db.OperationLogs.Add(new ST.Infra.EntityFramework.OperationLogs.OperationLog
			{
				CreatedAtUtc = entry.OccurredOnUtc.UtcDateTime,
				ServiceName = entry.ServiceName,
				TraceId = entry.TraceId,
				SpanId = entry.SpanId,
				UserId = entry.UserId,
				UserName = entry.UserName,
				OperationName = entry.OperationName,
				Path = entry.Path,
				Method = entry.Method,
				Ip = entry.Ip,
				StatusCode = entry.StatusCode,
				Success = entry.Success,
				DurationMs = entry.DurationMs,
				RequestJson = entry.RequestJson,
				ResponseJson = entry.ResponseJson,
				ExceptionType = entry.ExceptionType,
				ExceptionMessage = entry.ExceptionMessage,
				ExceptionStackTrace = entry.ExceptionStackTrace
			});

			await db.SaveChangesAsync().ConfigureAwait(false);

			if (_channel is not null)
			{
				await _channel.BasicAckAsync(eventArgs.DeliveryTag, false).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Consume operation log failed.");
			if (_channel is not null)
			{
				await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: _options.RequeueOnError).ConfigureAwait(false);
			}
		}
	}
}
