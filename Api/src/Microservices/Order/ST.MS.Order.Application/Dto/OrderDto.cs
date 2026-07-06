using ST.MS.Order.Domain.Enums;

namespace ST.MS.Order.Application.Dto;

/// <summary>
/// 订单响应。
/// </summary>
public sealed class OrderDto
{
	/// <summary>订单 ID</summary>
	public Guid Id { get; set; }

	/// <summary>订单号</summary>
	public string OrderNo { get; set; } = string.Empty;

	/// <summary>用户 ID</summary>
	public Guid UserId { get; set; }

	/// <summary>总金额</summary>
	public decimal TotalAmount { get; set; }

	/// <summary>订单状态</summary>
	public OrderStatus Status { get; set; }

	/// <summary>订单项</summary>
	public List<OrderItemDto> Items { get; set; } = [];

	/// <summary>创建时间</summary>
	public DateTime CreateTime { get; set; }

	/// <summary>取消原因</summary>
	public string? CancelReason { get; set; }
}

/// <summary>
/// 订单项响应。
/// </summary>
public sealed class OrderItemDto
{
	/// <summary>SKU ID</summary>
	public Guid SkuId { get; set; }

	/// <summary>商品名称</summary>
	public string ProductName { get; set; } = string.Empty;

	/// <summary>数量</summary>
	public int Quantity { get; set; }

	/// <summary>单价</summary>
	public decimal UnitPrice { get; set; }

	/// <summary>小计</summary>
	public decimal Subtotal { get; set; }
}
