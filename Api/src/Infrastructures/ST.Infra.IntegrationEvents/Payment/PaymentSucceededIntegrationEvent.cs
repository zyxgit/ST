using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.IntegrationEvents.Payment;

/// <summary>
/// 支付成功集成事件。
/// Payment Service 模拟支付成功后通过 Outbox 发布，
/// Order Service 消费此事件将订单状态更新为 Paid。
/// </summary>
public sealed record PaymentSucceededIntegrationEvent(
	Guid OrderId,
	Guid PaymentId,
	decimal Amount) : IntegrationEvent;
