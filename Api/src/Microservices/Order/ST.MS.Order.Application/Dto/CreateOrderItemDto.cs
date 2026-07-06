namespace ST.MS.Order.Application.Dto;

/// <summary>
/// 创建订单项请求。
/// </summary>
public sealed class CreateOrderItemDto
{
	/// <summary>SKU ID</summary>
	public Guid SkuId { get; set; }

	/// <summary>商品名称</summary>
	public string ProductName { get; set; } = string.Empty;

	/// <summary>数量</summary>
	public int Quantity { get; set; }

	/// <summary>单价</summary>
	public decimal UnitPrice { get; set; }
}
