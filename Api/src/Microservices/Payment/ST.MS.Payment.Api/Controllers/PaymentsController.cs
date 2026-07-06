using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.MS.Payment.Application.Dto;
using ST.MS.Payment.Application.Services;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Payment.Api.Controllers;

/// <summary>
/// 支付管理接口（模拟）。
/// </summary>
[AllowAnonymous]
public class PaymentsController : AbstractControllerBase
{
	private readonly IPaymentService _paymentService;
	private readonly ILogger<PaymentsController> _logger;

	public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
	{
		_paymentService = paymentService;
		_logger = logger;
	}

	/// <summary>
	/// 模拟支付成功
	/// </summary>
	[HttpPost("api/payments/mock/pay")]
	public async Task<ActionResult<PaymentDto>> MockPay([FromQuery] Guid orderId, CancellationToken ct)
	{
		var payment = await _paymentService.MockPayAsync(orderId, ct);

		_logger.LogInformation("Mock payment success via API. OrderId={OrderId}", orderId);

		return Ok(payment);
	}

	/// <summary>
	/// 模拟支付失败
	/// </summary>
	[HttpPost("api/payments/mock/fail")]
	public async Task<ActionResult<PaymentDto>> MockFail(
		[FromQuery] Guid orderId,
		[FromQuery] string reason = "模拟支付失败",
		CancellationToken ct = default)
	{
		var payment = await _paymentService.MockFailAsync(orderId, reason, ct);

		_logger.LogInformation("Mock payment failure via API. OrderId={OrderId} Reason={Reason}", orderId, reason);

		return Ok(payment);
	}

	/// <summary>
	/// 查询支付记录
	/// </summary>
	[HttpGet("api/payments/{orderId:guid}")]
	public async Task<ActionResult<PaymentDto>> GetPayment(Guid orderId, CancellationToken ct)
	{
		var payment = await _paymentService.GetPaymentAsync(orderId, ct);

		if (payment is null)
		{
			return NotFound(new { Error = "支付记录不存在", OrderId = orderId });
		}

		return Ok(payment);
	}
}
