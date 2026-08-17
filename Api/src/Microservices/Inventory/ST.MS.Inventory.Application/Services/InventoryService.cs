using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.Redis.Inventory;
using ST.MS.Inventory.Application.Dto;
using ST.MS.Inventory.Application.IServices;
using ST.MS.Inventory.Domain.Entities;
using ST.MS.Inventory.Domain.Enums;
using ST.MS.Inventory.Infra.DbContext;
using ST.Shared.Application.Services;
using ST.Shared.Exceptions;

namespace ST.MS.Inventory.Application.Services;

/// <summary>
/// 库存服务实现。
/// 双层防护：
/// 1. Redis Lua 原子预扣（热点层，拦截大部分并发请求）
/// 2. PostgreSQL 乐观锁（兜底层，保证最终一致性）
/// </summary>
public class InventoryService : AbstractAppService, IInventoryService
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

	public async Task<bool> FreezeInventoryAsync(Guid orderId, List<OrderItemData> items, bool skipRedisFreeze = false, CancellationToken ct = default)
	{
		// 幂等检查：同一订单已冻结则跳过
		var existingFreeze = await _dbContext.FreezeRecords
			.AnyAsync(r => r.OrderId == orderId && r.Status == FreezeStatus.Frozen, ct);
		if (existingFreeze)
		{
			_logger.LogWarning("订单 {OrderId} 库存已冻结，跳过。", orderId);
			return true;
		}

		// ── 第一层：Redis Lua 预扣（Order Service 已预扣时跳过） ──
		var redisReserved = new List<(Guid SkuId, int Quantity)>();

		if (!skipRedisFreeze)
		{
			foreach (var item in items)
			{
				var success = await _inventoryRedis.TryFreezeAsync(item.SkuId, item.Quantity, ct);
				if (!success)
				{
					// 区分缓存未命中和库存不足：键不存在则从 DB 同步后重试
					var keyExists = await _inventoryRedis.ExistsAsync(item.SkuId, ct);
					if (!keyExists)
					{
						_logger.LogWarning("Redis cache miss for SkuId={SkuId}, syncing from DB.", item.SkuId);
						await SyncStockFromDbAsync(item.SkuId, ct);
						success = await _inventoryRedis.TryFreezeAsync(item.SkuId, item.Quantity, ct);
					}

					if (!success)
					{
						// 库存真的不足，回滚已预扣的项
						_logger.LogWarning(
							"Redis 库存不足，SkuId={SkuId}。回滚 {Count} 个已预扣项。",
							item.SkuId, redisReserved.Count);

						foreach (var reserved in redisReserved)
						{
							await _inventoryRedis.ReleaseAsync(reserved.SkuId, reserved.Quantity, ct);
						}

						return false;
					}
				}

				redisReserved.Add((item.SkuId, item.Quantity));
			}
		}

		// ── 第二层：DB 乐观锁兜底 ──
		var freezeRecords = new List<InventoryFreezeRecord>();

		foreach (var item in items)
		{
			var affected = await _dbContext.Skus
				.Where(s => s.SkuId == item.SkuId && s.Available >= item.Quantity)
				.ExecuteUpdateAsync(s => s
					.SetProperty(x => x.Available, x => x.Available - item.Quantity)
					.SetProperty(x => x.Frozen, x => x.Frozen + item.Quantity), ct);

			if (affected == 0)
			{
				// DB 层库存不足（Redis 与 DB 数据不一致），回滚 Redis 预扣
				_logger.LogWarning(
					"DB 库存不足，SkuId={SkuId}。回滚 Redis 预扣。",
					item.SkuId);

				// 回滚本次所有 Redis 预扣
				foreach (var reserved in redisReserved)
				{
					await _inventoryRedis.ReleaseAsync(reserved.SkuId, reserved.Quantity, ct);
				}

				// 回滚已冻结的 DB 记录
				foreach (var record in freezeRecords)
				{
					await _dbContext.Skus
						.Where(s => s.SkuId == record.SkuId)
						.ExecuteUpdateAsync(s => s
							.SetProperty(x => x.Available, x => x.Available + record.Quantity)
							.SetProperty(x => x.Frozen, x => x.Frozen - record.Quantity), ct);
				}

				return false;
			}

			var freezeRecord = new InventoryFreezeRecord(orderId, item.SkuId, item.Quantity);
			freezeRecords.Add(freezeRecord);
			_dbContext.FreezeRecords.Add(freezeRecord);
		}

		await _dbContext.SaveChangesAsync(ct);

		// DB 冻结成功后，以 DB 为准同步 Redis（修复 Redis 与 DB 不一致）
		foreach (var item in items)
		{
			var sku = await _dbContext.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.SkuId == item.SkuId, ct);
			if (sku is not null)
			{
				await _inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, ct);
			}
		}

		_logger.LogInformation(
			"库存冻结成功，OrderId={OrderId}，商品数={ItemCount}（Redis + DB）",
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
			_logger.LogWarning("未找到订单 {OrderId} 的冻结记录。", orderId);
			return;
		}

		foreach (var record in freezeRecords)
		{
			// 释放 DB 库存：frozen → available
			await _dbContext.Skus
				.Where(s => s.SkuId == record.SkuId)
				.ExecuteUpdateAsync(s => s
					.SetProperty(x => x.Available, x => x.Available + record.Quantity)
					.SetProperty(x => x.Frozen, x => x.Frozen - record.Quantity), ct);

			// 释放 Redis 库存：frozen → available
			await _inventoryRedis.ReleaseAsync(record.SkuId, record.Quantity, ct);

			record.MarkReleased();
		}

		await _dbContext.SaveChangesAsync(ct);

		// DB 释放成功后，以 DB 为准同步 Redis
		foreach (var record in freezeRecords)
		{
			var sku = await _dbContext.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.SkuId == record.SkuId, ct);
			if (sku is not null)
			{
				await _inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, ct);
			}
		}

		_logger.LogInformation(
			"库存释放成功，OrderId={OrderId}，记录数={Count}（Redis + DB）",
			orderId, freezeRecords.Count);
	}

	public async Task<SkuDto?> GetSkuAsync(Guid skuId, CancellationToken ct = default)
	{
		var sku = await _dbContext.Skus.FirstOrDefaultAsync(s => s.SkuId == skuId, ct);
		if (sku is null) return null;

		// 优先读取 Redis 实时库存（抢购场景下 Redis 先于 DB 更新）
		var redisStock = await _inventoryRedis.GetStockAsync(skuId, ct);
		return MapToDto(sku, redisStock);
	}

	public async Task<List<SkuDto>> GetSkusAsync(CancellationToken ct = default)
	{
		var skus = await _dbContext.Skus
			.AsNoTracking()
			.OrderBy(s => s.ProductName)
			.ToListAsync(ct);

		// 逐个读取 Redis 实时库存
		var result = new List<SkuDto>();
		foreach (var sku in skus)
		{
			var redisStock = await _inventoryRedis.GetStockAsync(sku.SkuId, ct);
			result.Add(MapToDto(sku, redisStock));
		}

		return result;
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

		_logger.LogInformation("SKU 创建成功，SkuId={SkuId} 名称={Name} 库存={Stock}",
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

		_logger.LogInformation("库存增加成功，SkuId={SkuId} 增加={Added} 可用={Available}",
			skuId, quantity, sku.Available);

		return MapToDto(sku);
	}

	public async Task<SkuDto> DeductStockAsync(Guid skuId, int quantity, CancellationToken ct = default)
	{
		var sku = await _dbContext.Skus.FirstOrDefaultAsync(s => s.SkuId == skuId, ct)
			?? throw new BusinessException("SKU 不存在", errorCode: "SKU_NOT_FOUND");

		if (sku.Available < quantity)
		{
			throw new BusinessException(
				$"可用库存不足，当前可用: {sku.Available}，扣减: {quantity}",
				errorCode: "INSUFFICIENT_STOCK");
		}

		sku.Available -= quantity;
		await _dbContext.SaveChangesAsync(ct);

		// 同步更新 Redis
		await _inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, ct);

		_logger.LogInformation("库存扣减成功，SkuId={SkuId} 扣减={Deducted} 可用={Available}",
			skuId, quantity, sku.Available);

		return MapToDto(sku);
	}

	/// <summary>
	/// 从 DB 同步单个 SKU 库存到 Redis（缓存未命中时回源）。
	/// </summary>
	private async Task SyncStockFromDbAsync(Guid skuId, CancellationToken ct)
	{
		var sku = await _dbContext.Skus.AsNoTracking().FirstOrDefaultAsync(s => s.SkuId == skuId, ct);
		if (sku is not null)
		{
			await _inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, ct);
			_logger.LogInformation("Synced stock from DB. SkuId={SkuId} Available={Available}", skuId, sku.Available);
		}
	}

	/// <summary>
	/// 映射 SKU DTO。Redis 有实时数据时以 Redis 为准（抢购场景下 Redis 先于 DB 更新）。
	/// </summary>
	private static SkuDto MapToDto(Sku sku, (int Available, int Frozen, int Sold)? redisStock = null)
	{
		return new SkuDto
		{
			SkuId = sku.SkuId,
			ProductName = sku.ProductName,
			Available = redisStock?.Available ?? sku.Available,
			Frozen = redisStock?.Frozen ?? sku.Frozen,
			Sold = redisStock?.Sold ?? sku.Sold,
			TotalStock = sku.TotalStock
		};
	}
}
