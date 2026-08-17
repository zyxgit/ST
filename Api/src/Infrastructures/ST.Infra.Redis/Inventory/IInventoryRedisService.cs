namespace ST.Infra.Redis.Inventory;

/// <summary>
/// 库存 Redis 服务接口。
/// 基于 Lua 脚本实现原子性库存预扣，作为 DB 乐观锁之前的热点防护层。
/// </summary>
public interface IInventoryRedisService
{
	/// <summary>
	/// 尝试预扣库存（原子操作）。
	/// Redis 层 available >= quantity 时扣减并返回 true，否则返回 false。
	/// </summary>
	Task<bool> TryFreezeAsync(Guid skuId, int quantity, CancellationToken ct = default);

	/// <summary>
	/// 释放预扣库存（冻结 → 可用）。
	/// 用于订单取消或库存冻结失败时回滚。
	/// </summary>
	Task ReleaseAsync(Guid skuId, int quantity, CancellationToken ct = default);

	/// <summary>
	/// 确认售出（冻结 → 已售）。
	/// 用于支付成功后将冻结库存转为已售。
	/// </summary>
	Task ConfirmSoldAsync(Guid skuId, int quantity, CancellationToken ct = default);

	/// <summary>
	/// 从 DB 同步库存快照到 Redis（初始化或修复用）。
	/// </summary>
	Task SyncStockAsync(Guid skuId, int available, int frozen, int sold, CancellationToken ct = default);

	/// <summary>
	/// 获取 Redis 中的库存快照（available, frozen, sold）。
	/// 键不存在时返回 null。
	/// </summary>
	Task<(int Available, int Frozen, int Sold)?> GetStockAsync(Guid skuId, CancellationToken ct = default);

	/// <summary>
	/// 检查可用库存是否足够（只读，不扣减）。
	/// 用于下单前的同步预检查，避免超卖。
	/// Redis 键不存在时视为库存为 0。
	/// </summary>
	Task<bool> CheckAvailableAsync(Guid skuId, int quantity, CancellationToken ct = default);

	/// <summary>
	/// 检查指定 SKU 的 Redis 库存键是否存在。
	/// 用于区分"缓存未命中"和"库存不足"：TryFreezeAsync 返回 false 时，
	/// 若 ExistsAsync 为 false 则是缓存未命中（可回源 DB），否则是库存真的不足。
	/// </summary>
	Task<bool> ExistsAsync(Guid skuId, CancellationToken ct = default);
}
