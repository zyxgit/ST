using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.IntegrationEvents.Orders;

/// <summary>
/// 订单取消集成事件。
/// Order Service 取消订单后通过 Outbox 发布，
/// Inventory Service 消费此事件释放冻结库存。
/// </summary>
public sealed record OrderCanceledIntegrationEvent(
	Guid OrderId,
	string Reason) : IntegrationEvent;
