using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.EventBus.Events;

public sealed record UserLoginSucceededIntegrationEvent(
	Guid UserId,
	string Email,
	string LoginIp,
	DateTimeOffset LoginAtUtc) : IntegrationEvent;

