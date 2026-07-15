using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.IntegrationEvents.Payment;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Order.Domain.Enums;
using ST.MS.Order.Infra.DbContext;

namespace ST.MS.Order.Application.Services;

/// <summary>
/// 处理 PaymentSucceededIntegrationEvent。
/// 更新订单状态为 Paid，完成 Saga。
/// </summary>
public class PaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>
{
	private readonly OrderDbContext _dbContext;
	private readonly IInboxStore _inboxStore;
	private readonly ILogger<PaymentSucceededHandler> _logger;

	private const string Consumer = "OrderService";

	public PaymentSucceededHandler(
		OrderDbContext dbContext,
		IInboxStore inboxStore,
		ILogger<PaymentSucceededHandler> logger)
	{
		_dbContext = dbContext;
		_inboxStore = inboxStore;
		_logger = logger;
	}

	public async Task HandleAsync(PaymentSucceededIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		// 幂等检查
		if (await _inboxStore.ExistsAsync(@event.Id, Consumer, cancellationToken))
		{
			_logger.LogDebug("PaymentSucceeded event already processed. EventId={EventId}", @event.Id);
			return;
		}

		_inboxStore.Add(new InboxMessage
		{
			MessageId = @event.Id,
			Consumer = Consumer,
			EventType = nameof(PaymentSucceededIntegrationEvent),
			ReceivedAtUtc = DateTime.UtcNow
		});

		var order = await _dbContext.Orders
			.FirstOrDefaultAsync(o => o.Id == @event.OrderId, cancellationToken);

		if (order is null)
		{
			_logger.LogWarning("Order not found for PaymentSucceeded event. OrderId={OrderId}", @event.OrderId);
			await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		try
		{
			order.MarkPaid();
		}
		catch (InvalidOperationException ex)
		{
			// 订单处于无法支付的状态（如已取消/已失败），记录日志并标记已处理，不再重试
			_logger.LogWarning(ex, "Cannot mark order as Paid, skipping. OrderId={OrderId} Status={Status}",
				@event.OrderId, order.Status);
			await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		// 更新 Saga 状态
		if (order.SagaInstanceId.HasValue)
		{
			var saga = await _dbContext.SagaInstances
				.Include(s => s.Steps)
				.FirstOrDefaultAsync(s => s.Id == order.SagaInstanceId.Value, cancellationToken);

			if (saga is not null)
			{
				var step = saga.Steps.FirstOrDefault(s => s.StepName == "Paying");
				if (step is not null)
				{
					step.Status = "Completed";
				}
				saga.Complete();
			}
		}

		await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Order marked as Paid. OrderId={OrderId}", @event.OrderId);
	}
}
