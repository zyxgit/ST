using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.IntegrationEvents.Orders;

/// <summary>
/// 订单创建集成事件。
/// Order Service 创建订单后通过 Outbox 发布，
/// Inventory Service 消费此事件冻结库存。
/// </summary>
public sealed record OrderCreatedIntegrationEvent(
	Guid OrderId,
	Guid UserId,
	decimal TotalAmount,
	List<OrderItemData> Items) : IntegrationEvent;
