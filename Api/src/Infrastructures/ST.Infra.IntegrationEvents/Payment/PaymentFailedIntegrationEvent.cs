using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.IntegrationEvents.Payment;

/// <summary>
/// 支付失败集成事件。
/// Payment Service 模拟支付失败后通过 Outbox 发布，
/// Order Service 消费此事件取消订单并触发库存释放。
/// </summary>
public sealed record PaymentFailedIntegrationEvent(
	Guid OrderId,
	string Reason) : IntegrationEvent;
