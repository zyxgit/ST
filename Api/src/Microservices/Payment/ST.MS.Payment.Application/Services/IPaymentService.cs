using ST.MS.Payment.Application.Dto;

namespace ST.MS.Payment.Application.Services;

/// <summary>
/// 支付服务接口。
/// </summary>
public interface IPaymentService
{
	/// <summary>
	/// 模拟支付成功。
	/// </summary>
	Task<PaymentDto> MockPayAsync(Guid orderId, CancellationToken ct = default);

	/// <summary>
	/// 模拟支付失败。
	/// </summary>
	Task<PaymentDto> MockFailAsync(Guid orderId, string reason = "模拟支付失败", CancellationToken ct = default);

	/// <summary>
	/// 查询支付记录。
	/// </summary>
	Task<PaymentDto?> GetPaymentAsync(Guid orderId, CancellationToken ct = default);
}
