namespace ST.Infra.EventBus.Abstractions;

public abstract record IntegrationEvent
{
	public Guid Id { get; init; } = Guid.NewGuid();

	public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

	[JsonIgnore]
	public virtual string EventName => GetType().FullName ?? GetType().Name;
}

