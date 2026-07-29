using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ST.Infra.Redis.Inventory;
using ST.MS.Inventory.Infra.DbContext;

namespace ST.MS.Inventory.Application.Services;

/// <summary>
/// 应用启动时，将 DB 中的库存数据同步到 Redis。
/// 确保种子数据或其他直接写 DB 的库存变更在 Redis 中可用。
/// </summary>
public sealed class InventoryRedisSyncService : IHostedService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<InventoryRedisSyncService> _logger;

	public InventoryRedisSyncService(IServiceProvider serviceProvider, ILogger<InventoryRedisSyncService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	/// <summary>
	/// 在应用启动时（种子数据已通过 CodeFirstExecutors 执行），将所有 SKU 库存同步到 Redis。
	/// </summary>
	public async Task StartAsync(CancellationToken ct)
	{
		using var scope = _serviceProvider.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
		var inventoryRedis = scope.ServiceProvider.GetRequiredService<IInventoryRedisService>();

		var skus = await dbContext.Skus.AsNoTracking().ToListAsync(ct);

		foreach (var sku in skus)
		{
			await inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, ct);
			_logger.LogInformation(
				"SKU 同步到 Redis，SkuId={SkuId} 可用={Available} 冻结={Frozen} 已售={Sold}",
				sku.SkuId, sku.Available, sku.Frozen, sku.Sold);
		}

		_logger.LogInformation("库存 Redis 同步完成，SKU 数量={Count}", skus.Count);
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
