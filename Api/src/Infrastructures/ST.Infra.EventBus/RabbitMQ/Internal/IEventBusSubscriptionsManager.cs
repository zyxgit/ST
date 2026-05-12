using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.EventBus.RabbitMQ.Internal;

public interface IEventBusSubscriptionsManager
{
	bool IsEmpty { get; }

	void AddSubscription<TEvent, THandler>()
		where TEvent : IntegrationEvent
		where THandler : class, IIntegrationEventHandler<TEvent>;

	void RemoveSubscription<TEvent, THandler>()
		where TEvent : IntegrationEvent
		where THandler : class, IIntegrationEventHandler<TEvent>;

	bool HasSubscriptionsForEvent(string eventName);

	IReadOnlyCollection<Type> GetHandlersForEvent(string eventName);

	Type? GetEventTypeByName(string eventName);

	string GetEventKey<TEvent>()
		where TEvent : IntegrationEvent;
}

