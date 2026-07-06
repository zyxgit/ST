using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.IntegrationEvents.Inventory;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Order.Domain.Enums;
using ST.MS.Order.Infra.DbContext;

namespace ST.MS.Order.Application.Services;

/// <summary>
/// 处理 InventoryFreezeFailedIntegrationEvent。
/// 将订单标记为 Failed，Saga 标记失败。
/// </summary>
public class InventoryFreezeFailedHandler : IIntegrationEventHandler<InventoryFreezeFailedIntegrationEvent>
{
	private readonly OrderDbContext _dbContext;
	private readonly IInboxStore _inboxStore;
	private readonly ILogger<InventoryFreezeFailedHandler> _logger;

	private const string Consumer = "OrderService";

	public InventoryFreezeFailedHandler(
		OrderDbContext dbContext,
		IInboxStore inboxStore,
		ILogger<InventoryFreezeFailedHandler> logger)
	{
		_dbContext = dbContext;
		_inboxStore = inboxStore;
		_logger = logger;
	}

	public async Task HandleAsync(InventoryFreezeFailedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		// 幂等检查
		if (await _inboxStore.ExistsAsync(@event.Id, Consumer, cancellationToken))
		{
			_logger.LogDebug("InventoryFreezeFailed event already processed. EventId={EventId}", @event.Id);
			return;
		}

		// 记录 Inbox
		_inboxStore.Add(new InboxMessage
		{
			MessageId = @event.Id,
			Consumer = Consumer,
			EventType = nameof(InventoryFreezeFailedIntegrationEvent),
			ReceivedAtUtc = DateTime.UtcNow
		});

		// 更新订单状态
		var order = await _dbContext.Orders
			.FirstOrDefaultAsync(o => o.Id == @event.OrderId, cancellationToken);

		if (order is null)
		{
			_logger.LogWarning("Order not found for InventoryFreezeFailed event. OrderId={OrderId}", @event.OrderId);
			await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		order.MarkFailed(@event.Reason);

		// 更新 Saga 状态
		if (order.SagaInstanceId.HasValue)
		{
			var saga = await _dbContext.SagaInstances
				.Include(s => s.Steps)
				.FirstOrDefaultAsync(s => s.Id == order.SagaInstanceId.Value, cancellationToken);

			if (saga is not null)
			{
				var step = saga.Steps.FirstOrDefault(s => s.StepName == "InventoryFreezing");
				if (step is not null)
				{
					step.Status = "Failed";
				}
				saga.Fail(@event.Reason);
			}
		}

		// 标记 Inbox 已处理
		await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		OrderMetrics.SagaCompensated.Add(1);

		_logger.LogInformation("Order marked as Failed due to inventory freeze failure. OrderId={OrderId} Reason={Reason}",
			@event.OrderId, @event.Reason);
	}
}
