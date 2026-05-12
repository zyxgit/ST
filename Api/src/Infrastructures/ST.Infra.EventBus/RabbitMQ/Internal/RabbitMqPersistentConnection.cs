using ST.Infra.EventBus.RabbitMQ.Config;

namespace ST.Infra.EventBus.RabbitMQ.Internal;

public sealed class RabbitMqPersistentConnection : IRabbitMqPersistentConnection
{
	private readonly RabbitMqEventBusOptions _options;
	private readonly ILogger<RabbitMqPersistentConnection> _logger;
	private readonly object _syncRoot = new();

	private IConnection? _connection;
	private bool _disposed;

	public RabbitMqPersistentConnection(RabbitMqEventBusOptions options, ILogger<RabbitMqPersistentConnection> logger)
	{
		_options = options;
		_logger = logger;
	}

	public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

	public IModel CreateModel()
	{
		if (!IsConnected)
		{
			throw new InvalidOperationException("RabbitMQ connection is not available.");
		}

		return _connection!.CreateModel();
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
					DispatchConsumersAsync = true,
					AutomaticRecoveryEnabled = _options.AutomaticRecoveryEnabled,
					NetworkRecoveryInterval = TimeSpan.FromSeconds(_options.NetworkRecoveryIntervalSeconds),
				};

				_connection = factory.CreateConnection();

				_connection.ConnectionShutdown += (_, ea) =>
					_logger.LogWarning("RabbitMQ connection shutdown: {ReplyCode} {ReplyText}", ea.ReplyCode, ea.ReplyText);

				_connection.CallbackException += (_, ea) =>
					_logger.LogError(ea.Exception, "RabbitMQ connection callback exception.");

				_connection.ConnectionBlocked += (_, ea) =>
					_logger.LogWarning("RabbitMQ connection blocked: {Reason}", ea.Reason);

				_logger.LogInformation("RabbitMQ persistent connection established to {Host}:{Port}/{VHost}.",
					_options.HostName, _options.Port, _options.VirtualHost);

				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "RabbitMQ connection failed.");
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
			_logger.LogError(ex, "Error disposing RabbitMQ connection.");
		}
	}
}

