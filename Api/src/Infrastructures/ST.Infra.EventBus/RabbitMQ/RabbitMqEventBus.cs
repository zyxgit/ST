using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.EventBus.RabbitMQ.Config;
using ST.Infra.EventBus.RabbitMQ.Internal;

namespace ST.Infra.EventBus.RabbitMQ;

public sealed class RabbitMqEventBus : IEventBus, IDisposable
{
	private static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.Web);

	private readonly RabbitMqEventBusOptions _options;
	private readonly IRabbitMqPersistentConnection _persistentConnection;
	private readonly IEventBusSubscriptionsManager _subscriptionsManager;
	private readonly IServiceScopeFactory _serviceScopeFactory;
	private readonly ILogger<RabbitMqEventBus> _logger;
	private readonly object _consumerChannelLock = new();

	private IModel? _consumerChannel;
	private string? _consumerTag;

	public RabbitMqEventBus(
		RabbitMqEventBusOptions options,
		IRabbitMqPersistentConnection persistentConnection,
		IEventBusSubscriptionsManager subscriptionsManager,
		IServiceScopeFactory serviceScopeFactory,
		ILogger<RabbitMqEventBus> logger)
	{
		_options = options;
		_persistentConnection = persistentConnection;
		_subscriptionsManager = subscriptionsManager;
		_serviceScopeFactory = serviceScopeFactory;
		_logger = logger;

		EnsureBrokerObjects();
	}

	public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IntegrationEvent
	{
		EnsureConnectedOrThrow();

		var eventName = _subscriptionsManager.GetEventKey<TEvent>();
		var payload = JsonSerializer.Serialize(@event, @event.GetType(), DefaultJsonOptions);
		var body = Encoding.UTF8.GetBytes(payload);

		var attempt = 0;
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			try
			{
				using var channel = _persistentConnection.CreateModel();

				channel.ExchangeDeclare(
					exchange: _options.ExchangeName,
					type: ExchangeType.Direct,
					durable: _options.Durable,
					autoDelete: _options.AutoDelete);

				var properties = channel.CreateBasicProperties();
				properties.MessageId = @event.Id.ToString();
				properties.Type = eventName;
				properties.ContentType = "application/json";
				properties.DeliveryMode = _options.Durable ? (byte)2 : (byte)1;

				channel.BasicPublish(
					exchange: _options.ExchangeName,
					routingKey: eventName,
					mandatory: false,
					basicProperties: properties,
					body: body);

				return;
			}
			catch (Exception ex) when (attempt < _options.PublishRetryCount)
			{
				attempt++;
				var delay = TimeSpan.FromMilliseconds(200 * attempt * attempt);
				_logger.LogWarning(ex, "Publish failed (attempt {Attempt}/{Max}), retry in {Delay}ms. Event={EventName}",
					attempt, _options.PublishRetryCount, (int)delay.TotalMilliseconds, eventName);

				await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
			}
		}
	}

	public void Subscribe<TEvent, THandler>()
		where TEvent : IntegrationEvent
		where THandler : class, IIntegrationEventHandler<TEvent>
	{
		var eventName = _subscriptionsManager.GetEventKey<TEvent>();

		_subscriptionsManager.AddSubscription<TEvent, THandler>();
		DoInternalQueueBind(eventName);
		EnsureConsuming();
	}

	public void Unsubscribe<TEvent, THandler>()
		where TEvent : IntegrationEvent
		where THandler : class, IIntegrationEventHandler<TEvent>
	{
		var eventName = _subscriptionsManager.GetEventKey<TEvent>();

		_subscriptionsManager.RemoveSubscription<TEvent, THandler>();
		DoInternalQueueUnbind(eventName);
	}

	public void Dispose()
	{
		try
		{
			if (!string.IsNullOrWhiteSpace(_consumerTag) && _consumerChannel is { IsOpen: true })
			{
				_consumerChannel.BasicCancel(_consumerTag);
			}
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Error cancelling consumer.");
		}

		try
		{
			_consumerChannel?.Dispose();
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Error disposing consumer channel.");
		}

		_persistentConnection.Dispose();
	}

	private void EnsureBrokerObjects()
	{
		if (!_persistentConnection.TryConnect())
		{
			_logger.LogWarning("RabbitMQ not connected; broker objects will be declared on first successful connect.");
			return;
		}

		using var channel = _persistentConnection.CreateModel();

		channel.ExchangeDeclare(
			exchange: _options.ExchangeName,
			type: ExchangeType.Direct,
			durable: _options.Durable,
			autoDelete: _options.AutoDelete);

		channel.QueueDeclare(
			queue: _options.QueueName,
			durable: _options.Durable,
			exclusive: false,
			autoDelete: _options.AutoDelete,
			arguments: null);
	}

	private void EnsureConnectedOrThrow()
	{
		if (_persistentConnection.IsConnected)
		{
			return;
		}

		if (!_persistentConnection.TryConnect())
		{
			throw new InvalidOperationException("Unable to connect to RabbitMQ.");
		}
	}

	private void EnsureConsuming()
	{
		lock (_consumerChannelLock)
		{
			EnsureConnectedOrThrow();

			if (_consumerChannel is { IsOpen: true } && !string.IsNullOrWhiteSpace(_consumerTag))
			{
				return;
			}

			_consumerChannel?.Dispose();
			_consumerChannel = CreateConsumerChannel();

			var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
			consumer.Received += OnMessageReceivedAsync;

			_consumerTag = _consumerChannel.BasicConsume(
				queue: _options.QueueName,
				autoAck: false,
				consumer: consumer);

			_logger.LogInformation("RabbitMQ event bus consuming started. Exchange={Exchange} Queue={Queue}",
				_options.ExchangeName, _options.QueueName);
		}
	}

	private IModel CreateConsumerChannel()
	{
		var channel = _persistentConnection.CreateModel();

		channel.ExchangeDeclare(
			exchange: _options.ExchangeName,
			type: ExchangeType.Direct,
			durable: _options.Durable,
			autoDelete: _options.AutoDelete);

		channel.QueueDeclare(
			queue: _options.QueueName,
			durable: _options.Durable,
			exclusive: false,
			autoDelete: _options.AutoDelete,
			arguments: null);

		channel.BasicQos(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false);

		channel.CallbackException += (_, ea) =>
		{
			_logger.LogError(ea.Exception, "RabbitMQ consumer channel exception; recreating consumer channel.");
			lock (_consumerChannelLock)
			{
				_consumerTag = null;
				_consumerChannel?.Dispose();
				_consumerChannel = null;
			}
		};

		return channel;
	}

	private void DoInternalQueueBind(string eventName)
	{
		EnsureConnectedOrThrow();

		using var channel = _persistentConnection.CreateModel();

		channel.ExchangeDeclare(
			exchange: _options.ExchangeName,
			type: ExchangeType.Direct,
			durable: _options.Durable,
			autoDelete: _options.AutoDelete);

		channel.QueueDeclare(
			queue: _options.QueueName,
			durable: _options.Durable,
			exclusive: false,
			autoDelete: _options.AutoDelete,
			arguments: null);

		channel.QueueBind(
			queue: _options.QueueName,
			exchange: _options.ExchangeName,
			routingKey: eventName);
	}

	private void DoInternalQueueUnbind(string eventName)
	{
		EnsureConnectedOrThrow();

		using var channel = _persistentConnection.CreateModel();
		channel.QueueUnbind(
			queue: _options.QueueName,
			exchange: _options.ExchangeName,
			routingKey: eventName,
			arguments: null);
	}

	private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs eventArgs)
	{
		var eventName = eventArgs.BasicProperties?.Type;
		if (string.IsNullOrWhiteSpace(eventName))
		{
			eventName = eventArgs.RoutingKey;
		}

		var bodyText = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

		try
		{
			await ProcessEventAsync(eventName, bodyText).ConfigureAwait(false);

			_consumerChannel?.BasicAck(eventArgs.DeliveryTag, multiple: false);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing event. Event={EventName}", eventName);

			_consumerChannel?.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: _options.RequeueOnError);
		}
	}

	private async Task ProcessEventAsync(string eventName, string message)
	{
		if (!_subscriptionsManager.HasSubscriptionsForEvent(eventName))
		{
			_logger.LogDebug("No subscriptions for event {EventName}", eventName);
			return;
		}

		var eventType = _subscriptionsManager.GetEventTypeByName(eventName);
		if (eventType is null)
		{
			_logger.LogWarning("Unknown event type for {EventName}", eventName);
			return;
		}

		var integrationEvent = JsonSerializer.Deserialize(message, eventType, DefaultJsonOptions);
		if (integrationEvent is null)
		{
			_logger.LogWarning("Event deserialization returned null. Event={EventName}", eventName);
			return;
		}

		var handlers = _subscriptionsManager.GetHandlersForEvent(eventName);
		using var scope = _serviceScopeFactory.CreateScope();

		foreach (var handlerType in handlers)
		{
			var handler = scope.ServiceProvider.GetService(handlerType);
			if (handler is null)
			{
				_logger.LogWarning("Handler not resolved: {HandlerType}. Event={EventName}", handlerType.FullName, eventName);
				continue;
			}

			var handlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
			var method = handlerInterface.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync));
			if (method is null)
			{
				throw new InvalidOperationException($"Handler {handlerType.FullName} does not implement HandleAsync.");
			}

			var task = (Task?)method.Invoke(handler, new[] { integrationEvent, CancellationToken.None });
			if (task is null)
			{
				throw new InvalidOperationException($"Handler {handlerType.FullName} returned null task.");
			}

			await task.ConfigureAwait(false);
		}
	}
}

