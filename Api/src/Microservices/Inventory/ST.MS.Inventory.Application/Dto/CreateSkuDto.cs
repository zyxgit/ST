namespace ST.MS.Inventory.Application.Dto;

/// <summary>
/// 创建 SKU 请求。
/// </summary>
public sealed class CreateSkuDto
{
	/// <summary>SKU ID</summary>
	public Guid SkuId { get; set; }

	/// <summary>商品名称</summary>
	public string ProductName { get; set; } = string.Empty;

	/// <summary>初始库存</summary>
	public int InitialStock { get; set; }
}
