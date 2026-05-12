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
	private IModel? _channel;
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

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		ConnectAndStartConsume();
		return Task.CompletedTask;
	}

	public override Task StopAsync(CancellationToken cancellationToken)
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(_consumerTag) && _channel is { IsOpen: true })
			{
				_channel.BasicCancel(_consumerTag);
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Error cancelling consumer.");
		}

		try { _channel?.Dispose(); } catch { }
		try { _connection?.Dispose(); } catch { }

		return base.StopAsync(cancellationToken);
	}

	private void ConnectAndStartConsume()
	{
		var factory = new ConnectionFactory
		{
			HostName = _options.HostName,
			Port = _options.Port,
			UserName = _options.UserName,
			Password = _options.Password,
			VirtualHost = _options.VirtualHost,
			DispatchConsumersAsync = true,
			AutomaticRecoveryEnabled = true,
			NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
		};

		_connection = factory.CreateConnection();
		_channel = _connection.CreateModel();

		_channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Direct, durable: _options.Durable, autoDelete: _options.AutoDelete);

		var queueName = string.IsNullOrWhiteSpace(_options.QueueName) ? "st.operationlog.consumer" : _options.QueueName;
		_channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
		_channel.QueueBind(queue: queueName, exchange: _options.ExchangeName, routingKey: _options.RoutingKey);

		_channel.BasicQos(0, _options.PrefetchCount, global: false);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.Received += OnReceivedAsync;

		_consumerTag = _channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

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
				_channel?.BasicAck(eventArgs.DeliveryTag, false);
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

			_channel?.BasicAck(eventArgs.DeliveryTag, false);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Consume operation log failed.");
			_channel?.BasicNack(eventArgs.DeliveryTag, false, requeue: _options.RequeueOnError);
		}
	}
}
