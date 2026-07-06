namespace ST.MS.Inventory.Application.Dto;

/// <summary>
/// SKU 库存响应。
/// </summary>
public sealed class SkuDto
{
	/// <summary>SKU ID</summary>
	public Guid SkuId { get; set; }

	/// <summary>商品名称</summary>
	public string ProductName { get; set; } = string.Empty;

	/// <summary>可用库存</summary>
	public int Available { get; set; }

	/// <summary>冻结库存</summary>
	public int Frozen { get; set; }

	/// <summary>已售库存</summary>
	public int Sold { get; set; }

	/// <summary>总库存</summary>
	public int TotalStock { get; set; }
}
