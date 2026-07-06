namespace ST.MS.Order.Domain.Enums;

/// <summary>
/// 订单状态。
/// </summary>
public enum OrderStatus
{
	/// <summary>待处理（已创建，等待库存冻结）</summary>
	Pending = 0,

	/// <summary>库存已冻结，等待支付</summary>
	InventoryFrozen = 1,

	/// <summary>已支付</summary>
	Paid = 2,

	/// <summary>已取消</summary>
	Canceled = 3,

	/// <summary>失败（支付失败或库存不足）</summary>
	Failed = 4
}
