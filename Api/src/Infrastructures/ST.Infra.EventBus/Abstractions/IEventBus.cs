namespace ST.Infra.EventBus.Abstractions;

public interface IEventBus
{
	Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IntegrationEvent;

	void Subscribe<TEvent, THandler>()
		where TEvent : IntegrationEvent
		where THandler : class, IIntegrationEventHandler<TEvent>;

	void Unsubscribe<TEvent, THandler>()
		where TEvent : IntegrationEvent
		where THandler : class, IIntegrationEventHandler<TEvent>;
}

