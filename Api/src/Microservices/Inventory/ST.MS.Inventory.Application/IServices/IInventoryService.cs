using ST.Infra.IntegrationEvents.Orders;
using ST.MS.Inventory.Application.Dto;
using ST.Shared.Application;
using ST.Shared.Application.Dtos;

namespace ST.MS.Inventory.Application.IServices;

/// <summary>
/// 库存服务接口。
/// </summary>
public interface IInventoryService : IAppService
{
	/// <summary>
	/// 冻结库存（Redis 预扣 + DB 乐观锁）。
	/// </summary>
	/// <param name="orderId">订单 ID</param>
	/// <param name="items">订单项</param>
	/// <param name="skipRedisFreeze">跳过 Redis 预扣（Order Service 已完成时传 true）</param>
	/// <param name="ct">取消令牌</param>
	Task<bool> FreezeInventoryAsync(Guid orderId, List<OrderItemData> items, bool skipRedisFreeze = false, CancellationToken ct = default);

	/// <summary>
	/// 释放冻结库存。
	/// </summary>
	Task ReleaseInventoryAsync(Guid orderId, CancellationToken ct = default);

	/// <summary>
	/// 查询 SKU 库存。
	/// </summary>
	Task<SkuDto?> GetSkuAsync(Guid skuId, CancellationToken ct = default);

	/// <summary>
	/// SKU 列表查询。
	/// </summary>
	Task<List<SkuDto>> GetSkusAsync(CancellationToken ct = default);

	/// <summary>
	/// 创建 SKU。
	/// </summary>
	Task<SkuDto> CreateSkuAsync(CreateSkuDto input, CancellationToken ct = default);

	/// <summary>
	/// 增加库存。
	/// </summary>
	Task<SkuDto> IncreaseStockAsync(Guid skuId, int quantity, CancellationToken ct = default);

	/// <summary>
	/// 扣减库存。
	/// </summary>
	Task<SkuDto> DeductStockAsync(Guid skuId, int quantity, CancellationToken ct = default);
}
