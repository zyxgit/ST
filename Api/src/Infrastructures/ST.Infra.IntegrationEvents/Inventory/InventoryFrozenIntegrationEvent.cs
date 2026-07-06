using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.IntegrationEvents.Inventory;

/// <summary>
/// 库存冻结成功集成事件。
/// Inventory Service 冻结库存后通过 Outbox 发布，
/// Order Service 消费此事件将订单状态更新为 InventoryFrozen。
/// </summary>
public sealed record InventoryFrozenIntegrationEvent(
	Guid OrderId) : IntegrationEvent;
