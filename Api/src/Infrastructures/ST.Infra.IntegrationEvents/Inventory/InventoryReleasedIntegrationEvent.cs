using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.IntegrationEvents.Inventory;

/// <summary>
/// 库存释放集成事件。
/// Inventory Service 释放冻结库存后通过 Outbox 发布。
/// </summary>
public sealed record InventoryReleasedIntegrationEvent(
	Guid OrderId) : IntegrationEvent;
