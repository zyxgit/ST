using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.IntegrationEvents.Payment;
using ST.Infra.Redis.Inventory;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Inventory.Domain.Enums;
using ST.MS.Inventory.Infra.DbContext;

namespace ST.MS.Inventory.Application.Services;

/// <summary>
/// 处理 PaymentSucceededIntegrationEvent。
/// 将订单关联的冻结库存转为已售：DB (frozen → sold) + Redis (ConfirmSold)。
/// </summary>
public class InventoryPaymentSucceededHandler : IIntegrationEventHandler<PaymentSucceededIntegrationEvent>, ITransientDependency
{
	private readonly InventoryDbContext _dbContext;
	private readonly IInboxStore _inboxStore;
	private readonly IInventoryRedisService _inventoryRedis;
	private readonly ILogger<InventoryPaymentSucceededHandler> _logger;

	private const string Consumer = "InventoryService";

	public InventoryPaymentSucceededHandler(
		InventoryDbContext dbContext,
		IInboxStore inboxStore,
		IInventoryRedisService inventoryRedis,
		ILogger<InventoryPaymentSucceededHandler> logger)
	{
		_dbContext = dbContext;
		_inboxStore = inboxStore;
		_inventoryRedis = inventoryRedis;
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

		// 查找该订单的所有冻结记录
		var freezeRecords = await _dbContext.FreezeRecords
			.Where(r => r.OrderId == @event.OrderId && r.Status == FreezeStatus.Frozen)
			.ToListAsync(cancellationToken);

		if (freezeRecords.Count == 0)
		{
			_logger.LogWarning("支付成功但未找到订单 {OrderId} 的冻结记录。", @event.OrderId);
			await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return;
		}

		foreach (var record in freezeRecords)
		{
			// DB: frozen → sold
			await _dbContext.Skus
				.Where(s => s.SkuId == record.SkuId)
				.ExecuteUpdateAsync(s => s
					.SetProperty(x => x.Frozen, x => x.Frozen - record.Quantity)
					.SetProperty(x => x.Sold, x => x.Sold + record.Quantity), cancellationToken);

			// Redis: frozen → sold
			await _inventoryRedis.ConfirmSoldAsync(record.SkuId, record.Quantity, cancellationToken);

			record.MarkSold();
		}

		await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		// DB 更新后，以 DB 为准同步 Redis
		foreach (var record in freezeRecords)
		{
			var sku = await _dbContext.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.SkuId == record.SkuId, cancellationToken);
			if (sku is not null)
			{
				await _inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, cancellationToken);
			}
		}

		_logger.LogInformation(
			"库存确认售出，OrderId={OrderId}，记录数={Count}",
			@event.OrderId, freezeRecords.Count);
	}
}
