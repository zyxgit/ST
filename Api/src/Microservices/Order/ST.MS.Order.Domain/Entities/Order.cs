using ST.MS.Order.Domain.Enums;

namespace ST.MS.Order.Domain.Entities;

/// <summary>
/// 订单聚合根。
/// </summary>
public class Order : AggregateRoot, ITenantEntity
{
	/// <summary>订单号</summary>
	public string OrderNo { get; set; } = string.Empty;

	/// <summary>下单用户 ID</summary>
	public Guid UserId { get; set; }

	/// <summary>订单总金额</summary>
	public decimal TotalAmount { get; set; }

	/// <summary>订单状态</summary>
	public OrderStatus Status { get; set; } = OrderStatus.Pending;

	/// <summary>订单项</summary>
	public List<OrderItem> Items { get; set; } = [];

	/// <summary>关联的 Saga 实例 ID</summary>
	public Guid? SagaInstanceId { get; set; }

	/// <summary>取消原因（仅 Canceled 状态有值）</summary>
	public string? CancelReason { get; set; }

	/// <summary>租户 ID</summary>
	public Guid TenantId { get; set; }

	public Order()
	{
	}

	public Order(string orderNo, Guid userId, decimal totalAmount, List<OrderItem> items)
	{
		Id = Guid.CreateVersion7();
		OrderNo = orderNo;
		UserId = userId;
		TotalAmount = totalAmount;
		Items = items;
		Status = OrderStatus.Pending;
	}

	/// <summary>
	/// 标记库存已冻结。
	/// </summary>
	public void MarkInventoryFrozen()
	{
		if (Status != OrderStatus.Pending)
		{
			throw new InvalidOperationException($"Cannot mark inventory frozen for order in {Status} status.");
		}

		Status = OrderStatus.InventoryFrozen;
	}

	/// <summary>
	/// 标记已支付。
	/// </summary>
	public void MarkPaid()
	{
		if (Status != OrderStatus.InventoryFrozen)
		{
			throw new InvalidOperationException($"Cannot mark paid for order in {Status} status.");
		}

		Status = OrderStatus.Paid;
	}

	/// <summary>
	/// 取消订单。
	/// </summary>
	public void Cancel(string reason)
	{
		if (Status is OrderStatus.Paid or OrderStatus.Canceled)
		{
			throw new InvalidOperationException($"Cannot cancel order in {Status} status.");
		}

		Status = OrderStatus.Canceled;
		CancelReason = reason;
	}

	/// <summary>
	/// 标记失败。
	/// </summary>
	public void MarkFailed(string reason)
	{
		if (Status is OrderStatus.Paid or OrderStatus.Canceled)
		{
			throw new InvalidOperationException($"Cannot mark failed for order in {Status} status.");
		}

		Status = OrderStatus.Failed;
		CancelReason = reason;
	}
}
