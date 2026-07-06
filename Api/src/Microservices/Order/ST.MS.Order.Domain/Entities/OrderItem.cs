namespace ST.MS.Order.Domain.Entities;

/// <summary>
/// 订单项实体。
/// </summary>
public class OrderItem : Entity
{
	/// <summary>所属订单 ID</summary>
	public Guid OrderId { get; set; }

	/// <summary>SKU ID（预留 Inventory 服务集成）</summary>
	public Guid SkuId { get; set; }

	/// <summary>商品名称</summary>
	public string ProductName { get; set; } = string.Empty;

	/// <summary>数量</summary>
	public int Quantity { get; set; }

	/// <summary>单价</summary>
	public decimal UnitPrice { get; set; }

	/// <summary>小计金额</summary>
	public decimal Subtotal => Quantity * UnitPrice;

	public OrderItem()
	{
	}

	public OrderItem(Guid skuId, string productName, int quantity, decimal unitPrice)
	{
		Id = Guid.CreateVersion7();
		SkuId = skuId;
		ProductName = productName;
		Quantity = quantity;
		UnitPrice = unitPrice;
	}
}
