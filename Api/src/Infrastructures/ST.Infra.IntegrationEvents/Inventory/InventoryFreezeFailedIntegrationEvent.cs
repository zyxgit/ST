using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.IntegrationEvents.Inventory;

/// <summary>
/// 库存冻结失败集成事件。
/// Inventory Service 库存不足时通过 Outbox 发布，
/// Order Service 消费此事件将订单标记为 Failed。
/// </summary>
public sealed record InventoryFreezeFailedIntegrationEvent(
	Guid OrderId,
	string Reason) : IntegrationEvent;
