using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.IntegrationEvents.Payment;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Payment.Application.Dto;
using ST.MS.Payment.Domain.Enums;
using ST.MS.Payment.Infra.DbContext;
using ST.Shared.Exceptions;

namespace ST.MS.Payment.Application.Services;

/// <summary>
/// 支付服务实现。
/// 模拟支付成功/失败，通过 Outbox 发布集成事件。
/// </summary>
public class PaymentService : IPaymentService, ITransientDependency
{
	private readonly PaymentDbContext _dbContext;
	private readonly IOutboxStore _outboxStore;
	private readonly ILogger<PaymentService> _logger;

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public PaymentService(
		PaymentDbContext dbContext,
		IOutboxStore outboxStore,
		ILogger<PaymentService> logger)
	{
		_dbContext = dbContext;
		_outboxStore = outboxStore;
		_logger = logger;
	}

	public async Task<PaymentDto> MockPayAsync(Guid orderId, CancellationToken ct = default)
	{
		// 幂等检查
		var existing = await _dbContext.Payments
			.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

		if (existing is not null && existing.Status == PaymentStatus.Succeeded)
		{
			_logger.LogWarning("Payment already succeeded for OrderId={OrderId}", orderId);
			return MapToDto(existing);
		}

		// 查找待支付记录或创建新记录
		var payment = existing ?? new Domain.Entities.Payment(orderId, 0);
		if (existing is null)
		{
			_dbContext.Payments.Add(payment);
		}

		payment.MarkSucceeded();

		// 写入 Outbox：支付成功事件
		var successEvent = new PaymentSucceededIntegrationEvent(orderId, payment.Id, payment.Amount);
		_outboxStore.Add(new OutboxMessage
		{
			AggregateId = orderId,
			EventType = nameof(PaymentSucceededIntegrationEvent),
			Payload = JsonSerializer.Serialize(successEvent, successEvent.GetType(), JsonOptions),
			Status = OutboxStatus.Pending,
			OccurredAtUtc = DateTime.UtcNow
		});

		await _dbContext.SaveChangesAsync(ct);

		PaymentMetrics.Succeeded.Add(1);

		_logger.LogInformation("Payment succeeded for OrderId={OrderId} PaymentId={PaymentId}",
			orderId, payment.Id);

		return MapToDto(payment);
	}

	public async Task<PaymentDto> MockFailAsync(Guid orderId, string reason, CancellationToken ct = default)
	{
		var existing = await _dbContext.Payments
			.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

		if (existing is not null && existing.Status == PaymentStatus.Failed)
		{
			_logger.LogWarning("Payment already failed for OrderId={OrderId}", orderId);
			return MapToDto(existing);
		}

		var payment = existing ?? new Domain.Entities.Payment(orderId, 0);
		if (existing is null)
		{
			_dbContext.Payments.Add(payment);
		}

		payment.MarkFailed(reason);

		// 写入 Outbox：支付失败事件
		var failEvent = new PaymentFailedIntegrationEvent(orderId, reason);
		_outboxStore.Add(new OutboxMessage
		{
			AggregateId = orderId,
			EventType = nameof(PaymentFailedIntegrationEvent),
			Payload = JsonSerializer.Serialize(failEvent, failEvent.GetType(), JsonOptions),
			Status = OutboxStatus.Pending,
			OccurredAtUtc = DateTime.UtcNow
		});

		await _dbContext.SaveChangesAsync(ct);

		PaymentMetrics.Failed.Add(1);

		_logger.LogInformation("Payment failed for OrderId={OrderId} Reason={Reason}",
			orderId, reason);

		return MapToDto(payment);
	}

	public async Task<PaymentDto?> GetPaymentAsync(Guid orderId, CancellationToken ct = default)
	{
		var payment = await _dbContext.Payments
			.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);

		return payment is null ? null : MapToDto(payment);
	}

	private static PaymentDto MapToDto(Domain.Entities.Payment payment)
	{
		return new PaymentDto
		{
			Id = payment.Id,
			OrderId = payment.OrderId,
			Amount = payment.Amount,
			Status = payment.Status.ToString(),
			FailureReason = payment.FailureReason,
			CreateTime = payment.CreateTime
		};
	}
}
