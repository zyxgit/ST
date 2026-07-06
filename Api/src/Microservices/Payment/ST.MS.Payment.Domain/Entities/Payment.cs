using ST.MS.Payment.Domain.Enums;

namespace ST.MS.Payment.Domain.Entities;

/// <summary>
/// 支付记录聚合根。
/// 模拟支付服务，不接真实三方支付。
/// </summary>
public class Payment : AggregateRoot, ITenantEntity
{
	/// <summary>关联的订单 ID</summary>
	public Guid OrderId { get; set; }

	/// <summary>支付金额</summary>
	public decimal Amount { get; set; }

	/// <summary>支付状态</summary>
	public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

	/// <summary>失败原因</summary>
	public string? FailureReason { get; set; }

	/// <summary>租户 ID</summary>
	public Guid TenantId { get; set; }

	public Payment()
	{
	}

	public Payment(Guid orderId, decimal amount)
	{
		Id = Guid.CreateVersion7();
		OrderId = orderId;
		Amount = amount;
		Status = PaymentStatus.Pending;
	}

	/// <summary>标记支付成功。</summary>
	public void MarkSucceeded()
	{
		Status = PaymentStatus.Succeeded;
	}

	/// <summary>标记支付失败。</summary>
	public void MarkFailed(string reason)
	{
		Status = PaymentStatus.Failed;
		FailureReason = reason;
	}
}
