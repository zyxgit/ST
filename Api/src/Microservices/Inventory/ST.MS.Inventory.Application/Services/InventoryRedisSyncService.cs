using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Infra.Redis.Inventory;
using ST.Infra.Redis.Provider;
using ST.MS.Inventory.Application.Options;
using ST.MS.Inventory.Infra.DbContext;
using ST.Shared;

namespace ST.MS.Inventory.Application.Services;

/// <summary>
/// 库存 Redis 同步后台服务。
/// <list type="bullet">
///   <item>应用启动时执行一次全量同步</item>
///   <item>之后按配置间隔定时同步，兜底 TTL 过期、小范围数据漂移</item>
///   <item>检测到 Redis 断线恢复时立即触发一次同步</item>
/// </list>
/// </summary>
public sealed class InventoryRedisSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRedisClient _redisClient;
    private readonly IOptions<InventorySyncOptions> _options;
    private readonly ILogger<InventoryRedisSyncService> _logger;

    /// <summary>Redis 恢复信号，用于提前唤醒定时循环</summary>
    private TaskCompletionSource<bool>? _reconnectSignal;

    public InventoryRedisSyncService(
        IServiceScopeFactory scopeFactory,
        IRedisClient redisClient,
        IOptions<InventorySyncOptions> options,
        ILogger<InventoryRedisSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _redisClient = redisClient;
        _options = options;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken ct)
    {
        // 启动时立即同步一次（失败不阻止服务启动，后续定时任务会兜底）
        try
        {
            await SyncAllAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory Redis startup sync failed, will retry in next periodic cycle.");
        }

        await base.StartAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Inventory Redis periodic sync is disabled.");
            return;
        }

        var interval = TimeSpan.FromSeconds(_options.Value.SyncIntervalSeconds);
        _logger.LogInformation("Inventory Redis periodic sync started. Interval={Interval}", interval);

        // 订阅 Redis 恢复事件
        var connection = _redisClient.GetConnection();
        connection.ConnectionRestored += OnConnectionRestored;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                // 检查是否因 Redis 恢复被提前唤醒
                var isReconnect = false;
                if (_reconnectSignal is { Task.IsCompleted: true })
                {
                    isReconnect = true;
                    _reconnectSignal = null;
                }

                try
                {
                    if (isReconnect)
                    {
                        _logger.LogWarning("Redis connection restored, triggering immediate inventory sync.");
                    }

                    await SyncAllAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Inventory Redis periodic sync failed.");
                }
            }
        }
        finally
        {
            connection.ConnectionRestored -= OnConnectionRestored;
        }
    }

    private void OnConnectionRestored(object? sender, global::StackExchange.Redis.ConnectionFailedEventArgs e)
    {
        // 设置信号，让定时循环在下次迭代时立即执行同步
        _reconnectSignal?.TrySetResult(true);
    }

    /// <summary>
    /// 从数据库全量同步所有 SKU 库存到 Redis。
    /// 按租户分组同步，确保 Redis 键带上正确的租户前缀。
    /// </summary>
    private async Task SyncAllAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var inventoryRedis = scope.ServiceProvider.GetRequiredService<IInventoryRedisService>();

        // 先同步无租户的 SKU（TenantId 为空的兜底数据）
        var skus = await dbContext.Skus.AsNoTracking().ToListAsync(ct);

        // 按租户分组，逐租户设置 TenantContext 后同步，确保 Redis 键带正确的租户前缀
        var tenantGroups = skus.GroupBy(s => s.TenantId);
        var count = 0;

        foreach (var group in tenantGroups)
        {
            if (group.Key == Guid.Empty)
            {
                // 无租户数据，TenantContext 保持 null
                TenantContext.CurrentTenantId = null;
            }
            else
            {
                TenantContext.CurrentTenantId = group.Key;
            }

            foreach (var sku in group)
            {
                await inventoryRedis.SyncStockAsync(sku.SkuId, sku.Available, sku.Frozen, sku.Sold, ct);
                count++;
            }
        }

        // 同步完成后清除租户上下文，避免影响其他后台逻辑
        TenantContext.CurrentTenantId = null;

        _logger.LogInformation("Inventory Redis sync completed. SKU count={Count}", count);
    }
}
