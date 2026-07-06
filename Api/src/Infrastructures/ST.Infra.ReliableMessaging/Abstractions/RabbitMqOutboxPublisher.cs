using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// 基于 RabbitMQ 的 Outbox 消息投递实现。
/// 复用连接，每条消息使用独立 Channel 发送。
/// </summary>
public sealed class RabbitMqOutboxPublisher : IOutboxPublisher, IDisposable
{
	private readonly OutboxPublisherOptions _options;
	private readonly ILogger<RabbitMqOutboxPublisher> _logger;
	private readonly object _syncRoot = new();

	private IConnection? _connection;
	private bool _disposed;

	public RabbitMqOutboxPublisher(OutboxPublisherOptions options, ILogger<RabbitMqOutboxPublisher> logger)
	{
		_options = options;
		_logger = logger;
	}

	public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

	public async Task PublishAsync(OutboxMessage message, CancellationToken ct = default)
	{
		EnsureConnectedOrThrow();

		using var channel = await _connection!.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);

		await channel.ExchangeDeclareAsync(
			exchange: _options.ExchangeName,
			type: ExchangeType.Direct,
			durable: _options.Durable,
			autoDelete: false,
			cancellationToken: ct).ConfigureAwait(false);

		var body = Encoding.UTF8.GetBytes(message.Payload);

		var properties = new BasicProperties
		{
			MessageId = message.Id.ToString(),
			Type = message.EventType,
			ContentType = "application/json",
			Persistent = _options.Durable,
			CorrelationId = message.TraceId,
		};

		await channel.BasicPublishAsync(
			exchange: _options.ExchangeName,
			routingKey: message.EventType,
			mandatory: false,
			basicProperties: properties,
			body: body,
			cancellationToken: ct).ConfigureAwait(false);
	}

	public bool TryConnect()
	{
		if (_disposed)
		{
			return false;
		}

		lock (_syncRoot)
		{
			if (IsConnected)
			{
				return true;
			}

			try
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

				_connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();

				_connection.ConnectionShutdownAsync += (_, ea) =>
				{
					_logger.LogWarning("Outbox publisher RabbitMQ connection shutdown: {ReplyCode} {ReplyText}",
						ea.ReplyCode, ea.ReplyText);
					return Task.CompletedTask;
				};

				_logger.LogInformation(
					"Outbox publisher RabbitMQ connection established to {Host}:{Port}/{VHost}.",
					_options.HostName, _options.Port, _options.VirtualHost);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Outbox publisher RabbitMQ connection failed.");
				return false;
			}
		}
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		try
		{
			_connection?.Dispose();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error disposing outbox publisher RabbitMQ connection.");
		}
	}

	private void EnsureConnectedOrThrow()
	{
		if (IsConnected)
		{
			return;
		}

		if (!TryConnect())
		{
			throw new InvalidOperationException("Unable to connect to RabbitMQ for outbox publishing.");
		}
	}
}
