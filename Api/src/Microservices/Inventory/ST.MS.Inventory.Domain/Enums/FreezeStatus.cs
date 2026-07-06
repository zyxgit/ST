namespace ST.MS.Inventory.Domain.Enums;

/// <summary>
/// 库存冻结记录状态。
/// </summary>
public enum FreezeStatus
{
	/// <summary>已冻结</summary>
	Frozen = 0,

	/// <summary>已释放（订单取消）</summary>
	Released = 1,

	/// <summary>已转为已售（支付成功）</summary>
	Sold = 2
}
