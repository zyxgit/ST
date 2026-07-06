namespace ST.MS.Payment.Domain.Enums;

/// <summary>
/// 支付状态。
/// </summary>
public enum PaymentStatus
{
	/// <summary>待支付</summary>
	Pending = 0,

	/// <summary>支付成功</summary>
	Succeeded = 1,

	/// <summary>支付失败</summary>
	Failed = 2
}
