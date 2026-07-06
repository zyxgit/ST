using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.Redis.Inventory;
using ST.MS.Inventory.Application.Dto;
using ST.MS.Inventory.Domain.Entities;
using ST.MS.Inventory.Domain.Enums;
using ST.MS.Inventory.Infra.DbContext;
using ST.Shared.Exceptions;

namespace ST.MS.Inventory.Application.Services;

/// <summary>
/// 库存服务实现。
/// 双层防护：
/// 1. Redis Lua 原子预扣（热点层，拦截大部分并发请求）
/// 2. PostgreSQL 乐观锁（兜底层，保证最终一致性）
/// </summary>
public class InventoryService : IInventoryService, ITransientDependency
{
	private readonly InventoryDbContext _dbContext;
	private readonly IInventoryRedisService _inventoryRedis;
	private readonly ILogger<InventoryService> _logger;

	public InventoryService(
		InventoryDbContext dbContext,
		IInventoryRedisService inventoryRedis,
		ILogger<InventoryService> logger)
	{
		_dbContext = dbContext;
		_inventoryRedis = inventoryRedis;
		_logger = logger;
	}

	public async Task<bool> FreezeInventoryAsync(Guid orderId, List<OrderItemData> items, CancellationToken ct = default)
	{
		// 幂等检查：同一订单已冻结则跳过
		var existingFreeze = await _dbContext.FreezeRecords
			.AnyAsync(r => r.OrderId == orderId && r.Status == FreezeStatus.Frozen, ct);
		if (existingFreeze)
		{
			_logger.LogWarning("Inventory already frozen for order {OrderId}, skipping.", orderId);
			return true;
		}

		// ── 第一层：Redis Lua 预扣 ──
		var redisReserved = new List<(Guid SkuId, int Quantity)>();

		foreach (var item in items)
		{
			var success = await _inventoryRedis.TryFreezeAsync(item.SkuId, item.Quantity, ct);
			if (!success)
			{
				// Redis 库存不足，回滚已预扣的项
				_logger.LogWarning(
					"Redis insufficient stock for SkuId={SkuId}. Rolling back {Count} reserved items.",
					item.SkuId, redisReserved.Count);

				foreach (var reserved in redisReserved)
				{
					await _inventoryRedis.ReleaseAsync(reserved.SkuId, reserved.Quantity, ct);
				}

				return false;
			}

			redisReserved.Add((item.SkuId, item.Quantity));
		}

		// ── 第二层：DB 乐观锁兜底 ──
		var freezeRecords = new List<InventoryFreezeRecord>();

		foreach (var item in items)
		{
			var affected = await _dbContext.Database.ExecuteSqlRawAsync(
				"UPDATE skus SET available = available - {0}, frozen = frozen + {0} " +
				"WHERE sku_id = {1} AND available >= {0}",
				item.Quantity, item.SkuId, ct);

			if (affected == 0)
			{
				// DB 层库存不足（Redis 与 DB 数据不一致），回滚 Redis 预扣
				_logger.LogWarning(
					"DB insufficient stock for SkuId={SkuId}. Rolling back Redis pre-deduction.",
					item.SkuId);

				// 回滚本次所有 Redis 预扣
				foreach (var reserved in redisReserved)
				{
					await _inventoryRedis.ReleaseAsync(reserved.SkuId, reserved.Quantity, ct);
				}

				// 回滚已冻结的 DB 记录
				foreach (var record in freezeRecords)
				{
					await _dbContext.Database.ExecuteSqlRawAsync(
						"UPDATE skus SET available = available + {0}, frozen = frozen - {0} " +
						"WHERE sku_id = {1}",
						record.Quantity, record.SkuId, ct);
				}

				return false;
			}

			var freezeRecord = new InventoryFreezeRecord(orderId, item.SkuId, item.Quantity);
			freezeRecords.Add(freezeRecord);
			_dbContext.FreezeRecords.Add(freezeRecord);
		}

		await _dbContext.SaveChangesAsync(ct);

		_logger.LogInformation(
			"Inventory frozen for OrderId={OrderId}. Items={ItemCount} (Redis + DB)",
			orderId, items.Count);

		return true;
	}

	public async Task ReleaseInventoryAsync(Guid orderId, CancellationToken ct = default)
	{
		var freezeRecords = await _dbContext.FreezeRecords
			.Where(r => r.OrderId == orderId && r.Status == FreezeStatus.Frozen)
			.ToListAsync(ct);

		if (freezeRecords.Count == 0)
		{
			_logger.LogWarning("No frozen records found for OrderId={OrderId}.", orderId);
			return;
		}

		foreach (var record in freezeRecords)
		{
			// 释放 DB 库存：frozen → available
			await _dbContext.Database.ExecuteSqlRawAsync(
				"UPDATE skus SET available = available + {0}, frozen = frozen - {0} " +
				"WHERE sku_id = {1}",
				record.Quantity, record.SkuId, ct);

			// 释放 Redis 库存：frozen → available
			await _inventoryRedis.ReleaseAsync(record.SkuId, record.Quantity, ct);

			record.MarkReleased();
		}

		await _dbContext.SaveChangesAsync(ct);

		_logger.LogInformation(
			"Inventory released for OrderId={OrderId}. Records={Count} (Redis + DB)",
			orderId, freezeRecords.Count);
	}

	public async Task<SkuDto?> GetSkuAsync(Guid skuId, CancellationToken ct = default)
	{
		var sku = await _dbContext.Skus.FirstOrDefaultAsync(s => s.SkuId == skuId, ct);
		return sku is null ? null : MapToDto(sku);
	}

	public async Task<SkuDto> CreateSkuAsync(CreateSkuDto input, CancellationToken ct = default)
	{
		var exists = await _dbContext.Skus.AnyAsync(s => s.SkuId == input.SkuId, ct);
		if (exists)
		{
			throw new BusinessException("SKU 已存在", errorCode: "SKU_ALREADY_EXISTS");
		}

		var sku = new Sku(input.SkuId, input.ProductName, input.InitialStock);
		_dbContext.Skus.Add(sku);
		await _dbContext.SaveChangesAsync(ct);

		// 同步库存到 Redis
		await _inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, ct);

		_logger.LogInformation("SKU created. SkuId={SkuId} Name={Name} Stock={Stock}",
			sku.SkuId, sku.ProductName, sku.Available);

		return MapToDto(sku);
	}

	public async Task<SkuDto> IncreaseStockAsync(Guid skuId, int quantity, CancellationToken ct = default)
	{
		var sku = await _dbContext.Skus.FirstOrDefaultAsync(s => s.SkuId == skuId, ct)
			?? throw new BusinessException("SKU 不存在", errorCode: "SKU_NOT_FOUND");

		sku.Available += quantity;
		await _dbContext.SaveChangesAsync(ct);

		// 同步更新 Redis
		await _inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, ct);

		_logger.LogInformation("Stock increased. SkuId={SkuId} Added={Added} Available={Available}",
			skuId, quantity, sku.Available);

		return MapToDto(sku);
	}

	private static SkuDto MapToDto(Sku sku)
	{
		return new SkuDto
		{
			SkuId = sku.SkuId,
			ProductName = sku.ProductName,
			Available = sku.Available,
			Frozen = sku.Frozen,
			Sold = sku.Sold,
			TotalStock = sku.TotalStock
		};
	}
}
