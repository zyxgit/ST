using System.Text.Json;
using Microsoft.Extensions.Logging;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.IntegrationEvents.Inventory;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Inventory.Infra.DbContext;

namespace ST.MS.Inventory.Application.Services;

/// <summary>
/// 处理 OrderCanceledIntegrationEvent。
/// 释放冻结库存，发布 InventoryReleased。
/// </summary>
public class OrderCanceledHandler : IIntegrationEventHandler<OrderCanceledIntegrationEvent>
{
	private readonly InventoryDbContext _dbContext;
	private readonly IInboxStore _inboxStore;
	private readonly IOutboxStore _outboxStore;
	private readonly IInventoryService _inventoryService;
	private readonly ILogger<OrderCanceledHandler> _logger;

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private const string Consumer = "InventoryService";

	public OrderCanceledHandler(
		InventoryDbContext dbContext,
		IInboxStore inboxStore,
		IOutboxStore outboxStore,
		IInventoryService inventoryService,
		ILogger<OrderCanceledHandler> logger)
	{
		_dbContext = dbContext;
		_inboxStore = inboxStore;
		_outboxStore = outboxStore;
		_inventoryService = inventoryService;
		_logger = logger;
	}

	public async Task HandleAsync(OrderCanceledIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		// 幂等检查
		if (await _inboxStore.ExistsAsync(@event.Id, Consumer, cancellationToken))
		{
			_logger.LogDebug("OrderCanceled event already processed. EventId={EventId}", @event.Id);
			return;
		}

		// 记录 Inbox
		_inboxStore.Add(new InboxMessage
		{
			MessageId = @event.Id,
			Consumer = Consumer,
			EventType = nameof(OrderCanceledIntegrationEvent),
			ReceivedAtUtc = DateTime.UtcNow
		});

		// 释放冻结库存
		await _inventoryService.ReleaseInventoryAsync(@event.OrderId, cancellationToken);
		InventoryMetrics.Released.Add(1);

		// 发布 InventoryReleased
		var releasedEvent = new InventoryReleasedIntegrationEvent(@event.OrderId);
		_outboxStore.Add(new OutboxMessage
		{
			AggregateId = @event.OrderId,
			EventType = typeof(InventoryReleasedIntegrationEvent).FullName!,
			Payload = JsonSerializer.Serialize(releasedEvent, releasedEvent.GetType(), JsonOptions),
			Status = OutboxStatus.Pending,
			OccurredAtUtc = DateTime.UtcNow
		});

		// 标记 Inbox 已处理
		await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);

		// 同一事务保存
		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Inventory released for OrderId={OrderId}", @event.OrderId);
	}
}
