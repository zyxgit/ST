using System.Text.Json;
using Microsoft.Extensions.Logging;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.IntegrationEvents.Inventory;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Inventory.Application.IServices;
using ST.MS.Inventory.Infra.DbContext;

namespace ST.MS.Inventory.Application.Services;

/// <summary>
/// 处理 OrderCreatedIntegrationEvent。
/// 冻结库存，成功则发布 InventoryFrozen，失败则发布 InventoryFreezeFailed。
/// 使用 Inbox 幂等 + Outbox 可靠发布，与冻结操作同一事务。
/// </summary>
public class OrderCreatedHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>, ITransientDependency
{
	private readonly InventoryDbContext _dbContext;
	private readonly IInboxStore _inboxStore;
	private readonly IOutboxStore _outboxStore;
	private readonly IInventoryService _inventoryService;
	private readonly ILogger<OrderCreatedHandler> _logger;

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private const string Consumer = "InventoryService";

	public OrderCreatedHandler(
		InventoryDbContext dbContext,
		IInboxStore inboxStore,
		IOutboxStore outboxStore,
		IInventoryService inventoryService,
		ILogger<OrderCreatedHandler> logger)
	{
		_dbContext = dbContext;
		_inboxStore = inboxStore;
		_outboxStore = outboxStore;
		_inventoryService = inventoryService;
		_logger = logger;
	}

	public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		// 幂等检查
		if (await _inboxStore.ExistsAsync(@event.Id, Consumer, cancellationToken))
		{
			_logger.LogDebug("OrderCreated 事件已处理，跳过。EventId={EventId}", @event.Id);
			return;
		}

		// 记录 Inbox
		_inboxStore.Add(new InboxMessage
		{
			MessageId = @event.Id,
			Consumer = Consumer,
			EventType = nameof(OrderCreatedIntegrationEvent),
			ReceivedAtUtc = DateTime.UtcNow
		});

		// 冻结库存（如果 Order Service 已完成 Redis 预扣，则跳过 Redis 层）
		var success = await _inventoryService.FreezeInventoryAsync(
			@event.OrderId, @event.Items, skipRedisFreeze: @event.RedisPreFrozen, cancellationToken);

		if (success)
		{
			// 冻结成功 → 发布 InventoryFrozen
			var frozenEvent = new InventoryFrozenIntegrationEvent(@event.OrderId);
			_outboxStore.Add(new OutboxMessage
			{
				AggregateId = @event.OrderId,
				EventType = typeof(InventoryFrozenIntegrationEvent).FullName!,
				Payload = JsonSerializer.Serialize(frozenEvent, frozenEvent.GetType(), JsonOptions),
				Status = OutboxStatus.Pending,
				OccurredAtUtc = DateTime.UtcNow
			});

			_logger.LogInformation("库存冻结成功，OrderId={OrderId}", @event.OrderId);
		}
		else
		{
			// 冻结失败 → 发布 InventoryFreezeFailed
			var failedEvent = new InventoryFreezeFailedIntegrationEvent(@event.OrderId, "库存不足");
			_outboxStore.Add(new OutboxMessage
			{
				AggregateId = @event.OrderId,
				EventType = typeof(InventoryFreezeFailedIntegrationEvent).FullName!,
				Payload = JsonSerializer.Serialize(failedEvent, failedEvent.GetType(), JsonOptions),
				Status = OutboxStatus.Pending,
				OccurredAtUtc = DateTime.UtcNow
			});

			_logger.LogWarning("库存冻结失败，OrderId={OrderId}", @event.OrderId);
		}

		// 标记 Inbox 已处理
		await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);

		// 同一事务保存（Inbox + FreezeRecords + Outbox）
		await _dbContext.SaveChangesAsync(cancellationToken);
	}
}
