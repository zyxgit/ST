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
/// 处理 InventoryFrozenIntegrationEvent。
/// 更新订单状态为 InventoryFrozen，推进 Saga 步骤。
/// </summary>
public class InventoryFrozenHandler : IIntegrationEventHandler<InventoryFrozenIntegrationEvent>
{
	private readonly OrderDbContext _dbContext;
	private readonly IInboxStore _inboxStore;
	private readonly ILogger<InventoryFrozenHandler> _logger;

	private const string Consumer = "OrderService";

	public InventoryFrozenHandler(
		OrderDbContext dbContext,
		IInboxStore inboxStore,
		ILogger<InventoryFrozenHandler> logger)
	{
		_dbContext = dbContext;
		_inboxStore = inboxStore;
		_logger = logger;
	}

	public async Task HandleAsync(InventoryFrozenIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		// 幂等检查
		if (await _inboxStore.ExistsAsync(@event.Id, Consumer, cancellationToken))
		{
			_logger.LogDebug("InventoryFrozen event already processed. EventId={EventId}", @event.Id);
			return;
		}

		// 记录 Inbox
		_inboxStore.Add(new InboxMessage
		{
			MessageId = @event.Id,
			Consumer = Consumer,
			EventType = nameof(InventoryFrozenIntegrationEvent),
			ReceivedAtUtc = DateTime.UtcNow
		});

		// 更新订单状态
		var order = await _dbContext.Orders
			.FirstOrDefaultAsync(o => o.Id == @event.OrderId, cancellationToken);

		if (order is null)
		{
			_logger.LogWarning("Order not found for InventoryFrozen event. OrderId={OrderId}", @event.OrderId);
			await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		order.MarkInventoryFrozen();

		// 更新 Saga 步骤
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
					step.Status = "Completed";
				}
				saga.AdvanceTo("Paying");
			}
		}

		// 标记 Inbox 已处理
		await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Order updated to InventoryFrozen. OrderId={OrderId}", @event.OrderId);
	}
}
