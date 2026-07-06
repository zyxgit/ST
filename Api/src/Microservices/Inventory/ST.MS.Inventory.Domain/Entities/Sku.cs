namespace ST.MS.Inventory.Domain.Entities;

/// <summary>
/// SKU 库存聚合根。
/// 管理可用库存、冻结库存和已售库存。
/// </summary>
public class Sku : AggregateRoot, ITenantEntity
{
	/// <summary>SKU ID（与 Order 中的 SkuId 对应）</summary>
	public Guid SkuId { get; set; }

	/// <summary>商品名称</summary>
	public string ProductName { get; set; } = string.Empty;

	/// <summary>可用库存</summary>
	public int Available { get; set; }

	/// <summary>冻结库存（已下单未支付）</summary>
	public int Frozen { get; set; }

	/// <summary>已售库存（已支付）</summary>
	public int Sold { get; set; }

	/// <summary>行版本号（乐观锁）</summary>
	public uint Version { get; set; }

	/// <summary>租户 ID</summary>
	public Guid TenantId { get; set; }

	public Sku()
	{
	}

	public Sku(Guid skuId, string productName, int initialStock)
	{
		Id = Guid.CreateVersion7();
		SkuId = skuId;
		ProductName = productName;
		Available = initialStock;
		Frozen = 0;
		Sold = 0;
	}

	/// <summary>
	/// 总库存 = 可用 + 冻结 + 已售。
	/// </summary>
	public int TotalStock => Available + Frozen + Sold;
}
