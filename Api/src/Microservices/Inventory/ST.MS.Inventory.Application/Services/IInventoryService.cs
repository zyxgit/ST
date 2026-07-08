using ST.Infra.IntegrationEvents.Orders;
using ST.MS.Inventory.Application.Dto;
using ST.Shared.Application.Dtos;

namespace ST.MS.Inventory.Application.Services;

/// <summary>
/// 库存服务接口。
/// </summary>
public interface IInventoryService
{
	/// <summary>
	/// 冻结库存（DB 乐观锁）。
	/// </summary>
	Task<bool> FreezeInventoryAsync(Guid orderId, List<OrderItemData> items, CancellationToken ct = default);

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
}
