namespace ST.MS.Payment.Application.Dto;

/// <summary>
/// 支付记录响应。
/// </summary>
public sealed class PaymentDto
{
	/// <summary>支付 ID</summary>
	public Guid Id { get; set; }

	/// <summary>订单 ID</summary>
	public Guid OrderId { get; set; }

	/// <summary>支付金额</summary>
	public decimal Amount { get; set; }

	/// <summary>支付状态</summary>
	public string Status { get; set; } = string.Empty;

	/// <summary>失败原因</summary>
	public string? FailureReason { get; set; }

	/// <summary>创建时间</summary>
	public DateTime CreateTime { get; set; }
}
