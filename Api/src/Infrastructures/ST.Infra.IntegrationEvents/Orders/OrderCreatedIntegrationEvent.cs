using ST.Infra.EventBus.Abstractions;

namespace ST.Infra.IntegrationEvents.Orders;

/// <summary>
/// 订单创建集成事件。
/// Order Service 创建订单后通过 Outbox 发布，
/// Inventory Service 消费此事件冻结库存。
/// </summary>
/// <param name="OrderId">订单 ID</param>
/// <param name="UserId">用户 ID</param>
/// <param name="TotalAmount">订单总金额</param>
/// <param name="Items">订单项</param>
/// <param name="RedisPreFrozen">是否已在 Order Service 完成 Redis 预扣。为 true 时 Inventory Service 跳过 Redis 冻结，仅做 DB 兜底。</param>
public sealed record OrderCreatedIntegrationEvent(
	Guid OrderId,
	Guid UserId,
	decimal TotalAmount,
	List<OrderItemData> Items,
	bool RedisPreFrozen = false) : IntegrationEvent;
