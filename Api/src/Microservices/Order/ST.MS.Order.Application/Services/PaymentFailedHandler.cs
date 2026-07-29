using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.IntegrationEvents.Inventory;
using ST.Infra.IntegrationEvents.Payment;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Order.Domain.Enums;
using ST.MS.Order.Infra.DbContext;

namespace ST.MS.Order.Application.Services;

/// <summary>
/// 处理 PaymentFailedIntegrationEvent。
/// 取消订单并触发库存释放（发布 OrderCanceledIntegrationEvent）。
/// </summary>
public class PaymentFailedHandler : IIntegrationEventHandler<PaymentFailedIntegrationEvent>, ITransientDependency
{
	private readonly OrderDbContext _dbContext;
	private readonly IInboxStore _inboxStore;
	private readonly IOutboxStore _outboxStore;
	private readonly ILogger<PaymentFailedHandler> _logger;

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private const string Consumer = "OrderService";

	public PaymentFailedHandler(
		OrderDbContext dbContext,
		IInboxStore inboxStore,
		IOutboxStore outboxStore,
		ILogger<PaymentFailedHandler> logger)
	{
		_dbContext = dbContext;
		_inboxStore = inboxStore;
		_outboxStore = outboxStore;
		_logger = logger;
	}

	public async Task HandleAsync(PaymentFailedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		// 幂等检查
		if (await _inboxStore.ExistsAsync(@event.Id, Consumer, cancellationToken))
		{
			_logger.LogDebug("PaymentFailed event already processed. EventId={EventId}", @event.Id);
			return;
		}

		_inboxStore.Add(new InboxMessage
		{
			MessageId = @event.Id,
			Consumer = Consumer,
			EventType = nameof(PaymentFailedIntegrationEvent),
			ReceivedAtUtc = DateTime.UtcNow
		});

		var order = await _dbContext.Orders
			.FirstOrDefaultAsync(o => o.Id == @event.OrderId, cancellationToken);

		if (order is null)
		{
			_logger.LogWarning("Order not found for PaymentFailed event. OrderId={OrderId}", @event.OrderId);
			await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		// 取消订单
		order.Cancel($"支付失败: {@event.Reason}");

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
					step.Status = "Failed";
				}
				saga.StartCompensation(@event.Reason);
			}
		}

		// 发布 OrderCanceled 事件触发库存释放
		var cancelEvent = new ST.Infra.IntegrationEvents.Orders.OrderCanceledIntegrationEvent(
			order.Id, $"支付失败: {@event.Reason}");
		_outboxStore.Add(new OutboxMessage
		{
			AggregateId = order.Id,
			EventType = typeof(ST.Infra.IntegrationEvents.Orders.OrderCanceledIntegrationEvent).FullName!,
			Payload = JsonSerializer.Serialize(cancelEvent, cancelEvent.GetType(), JsonOptions),
			Status = OutboxStatus.Pending,
			OccurredAtUtc = DateTime.UtcNow
		});

		await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		OrderMetrics.SagaCompensated.Add(1);

		_logger.LogInformation("Order canceled due to payment failure. OrderId={OrderId} Reason={Reason}",
			@event.OrderId, @event.Reason);
	}
}
