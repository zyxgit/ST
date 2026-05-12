using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.EventBus.RabbitMQ.Internal;

public sealed class InMemoryEventBusSubscriptionsManager : IEventBusSubscriptionsManager
{
	private readonly ConcurrentDictionary<string, ConcurrentDictionary<Type, byte>> _handlers = new();
	private readonly ConcurrentDictionary<string, Type> _eventTypes = new();

	public bool IsEmpty => _handlers.IsEmpty;

	public void AddSubscription<TEvent, THandler>()
		where TEvent : IntegrationEvent
		where THandler : class, IIntegrationEventHandler<TEvent>
	{
		var eventName = GetEventKey<TEvent>();
		var handlerType = typeof(THandler);
		_eventTypes.TryAdd(eventName, typeof(TEvent));

		var handlersForEvent = _handlers.GetOrAdd(eventName, _ => new ConcurrentDictionary<Type, byte>());
		handlersForEvent.TryAdd(handlerType, 0);
	}

	public void RemoveSubscription<TEvent, THandler>()
		where TEvent : IntegrationEvent
		where THandler : class, IIntegrationEventHandler<TEvent>
	{
		var eventName = GetEventKey<TEvent>();
		if (!_handlers.TryGetValue(eventName, out var handlersForEvent))
		{
			return;
		}

		handlersForEvent.TryRemove(typeof(THandler), out _);
		if (handlersForEvent.IsEmpty)
		{
			_handlers.TryRemove(eventName, out _);
			_eventTypes.TryRemove(eventName, out _);
		}
	}

	public bool HasSubscriptionsForEvent(string eventName)
		=> _handlers.TryGetValue(eventName, out var handlersForEvent) && !handlersForEvent.IsEmpty;

	public IReadOnlyCollection<Type> GetHandlersForEvent(string eventName)
	{
		if (!_handlers.TryGetValue(eventName, out var handlersForEvent))
		{
			return Array.Empty<Type>();
		}

		return handlersForEvent.Keys.ToArray();
	}

	public Type? GetEventTypeByName(string eventName)
	{
		_eventTypes.TryGetValue(eventName, out var eventType);
		return eventType;
	}

	public string GetEventKey<TEvent>()
		where TEvent : IntegrationEvent
	{
		var type = typeof(TEvent);
		return type.FullName ?? type.Name;
	}
}

