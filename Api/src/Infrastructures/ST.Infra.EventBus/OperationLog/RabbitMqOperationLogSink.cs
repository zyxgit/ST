using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ST.Shared.OperationLog;

namespace ST.Infra.EventBus.OperationLog;

public sealed class RabbitMqOperationLogConnection : IDisposable
{
	private readonly RabbitMqOperationLogOptions _options;
	private readonly ILogger<RabbitMqOperationLogConnection> _logger;
	private readonly object _syncRoot = new();

	private IConnection? _connection;
	private bool _disposed;

	public RabbitMqOperationLogConnection(RabbitMqOperationLogOptions options, ILogger<RabbitMqOperationLogConnection> logger)
	{
		_options = options;
		_logger = logger;
	}

	public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

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
					DispatchConsumersAsync = true,
					AutomaticRecoveryEnabled = true,
					NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
				};

				_connection = factory.CreateConnection();
				_logger.LogInformation("RabbitMQ operationlog connection established to {Host}:{Port}/{VHost}.",
					_options.HostName, _options.Port, _options.VirtualHost);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "RabbitMQ operationlog connection failed.");
				return false;
			}
		}
	}

	public IModel CreateModel()
	{
		if (!IsConnected)
		{
			throw new InvalidOperationException("RabbitMQ operationlog connection is not available.");
		}

		return _connection!.CreateModel();
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
			_logger.LogError(ex, "Error disposing RabbitMQ operationlog connection.");
		}
	}
}

public sealed class RabbitMqOperationLogSink : IOperationLogSink
{
	public string Name => "rabbitmq";

	private readonly RabbitMqOperationLogOptions _options;
	private readonly RabbitMqOperationLogConnection _connection;
	private readonly ILogger<RabbitMqOperationLogSink> _logger;

	public RabbitMqOperationLogSink(
		RabbitMqOperationLogOptions options,
		RabbitMqOperationLogConnection connection,
		ILogger<RabbitMqOperationLogSink> logger)
	{
		_options = options;
		_connection = connection;
		_logger = logger;
	}

	public async ValueTask EnqueueAsync(OperationLogEntry entry, CancellationToken cancellationToken = default)
	{
		if (!_connection.IsConnected && !_connection.TryConnect())
		{
			return;
		}

		var payload = JsonSerializer.Serialize(entry, new JsonSerializerOptions(JsonSerializerDefaults.Web));
		var body = System.Text.Encoding.UTF8.GetBytes(payload);

		var attempt = 0;
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				using var channel = _connection.CreateModel();

				channel.ExchangeDeclare(
					exchange: _options.ExchangeName,
					type: ExchangeType.Direct,
					durable: _options.Durable,
					autoDelete: _options.AutoDelete);

				var props = channel.CreateBasicProperties();
				props.ContentType = "application/json";
				props.DeliveryMode = _options.Durable ? (byte)2 : (byte)1;
				props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
				props.Type = nameof(OperationLogEntry);

				channel.BasicPublish(
					exchange: _options.ExchangeName,
					routingKey: _options.RoutingKey,
					mandatory: false,
					basicProperties: props,
					body: body);

				return;
			}
			catch (Exception ex) when (attempt < _options.PublishRetryCount)
			{
				attempt++;
				var delay = TimeSpan.FromMilliseconds(200 * attempt * attempt);
				_logger.LogWarning(ex, "Publish operation log failed (attempt {Attempt}/{Max}), retry in {Delay}ms.",
					attempt, _options.PublishRetryCount, (int)delay.TotalMilliseconds);
				await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				return;
			}
		}
	}
}

public static class RabbitMqOperationLogServiceCollectionExtensions
{
	/// <summary>
	/// 注册操作日志 RabbitMQ Sink（发布端）。配置节点默认：RabbitMQ:OperationLog
	/// </summary>
	public static IServiceCollection AddRabbitMqOperationLogSink(
		this IServiceCollection services,
		IConfiguration configuration,
		string sectionName = "RabbitMQ:OperationLog")
	{
		var options = new RabbitMqOperationLogOptions();
		configuration.GetSection(sectionName).Bind(options);

		services.AddSingleton(options);
		services.AddSingleton<RabbitMqOperationLogConnection>();

		services.AddSingleton<IOperationLogSink, RabbitMqOperationLogSink>();
		return services;
	}
}
