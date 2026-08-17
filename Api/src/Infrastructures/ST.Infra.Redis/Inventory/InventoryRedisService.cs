using Microsoft.Extensions.Logging;
using ST.Infra.Redis.Provider;
using ST.Shared;

namespace ST.Infra.Redis.Inventory;

/// <summary>
/// 基于 Redis Lua 脚本的库存预扣实现。
/// 所有操作均为原子性，保证高并发下的数据一致性。
/// </summary>
public sealed class InventoryRedisService : IInventoryRedisService
{
	private readonly IRedisClient _redisClient;
	private readonly ILogger<InventoryRedisService> _logger;

	/// <summary>Redis 键前缀</summary>
	private const string KeyPrefix = "inventory:sku";

	/// <summary>默认 TTL 24 小时（兜底防泄漏）</summary>
	private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);

	public InventoryRedisService(IRedisClient redisClient, ILogger<InventoryRedisService> logger)
	{
		_redisClient = redisClient;
		_logger = logger;
	}

	/// <summary>
	/// Lua 脚本：原子预扣库存。
	/// available >= quantity 时扣减 available 并增加 frozen，返回 1；否则返回 0。
	///
	/// KEYS[1] = available 键
	/// KEYS[2] = frozen 键
	/// ARGV[1] = quantity
	/// </summary>
	private const string FreezeScript = @"
local available = tonumber(redis.call('GET', KEYS[1]) or '0')
local quantity = tonumber(ARGV[1])
if available >= quantity then
    redis.call('DECRBY', KEYS[1], quantity)
    redis.call('INCRBY', KEYS[2], quantity)
    return 1
else
    return 0
end";

	/// <summary>
	/// Lua 脚本：释放预扣库存（frozen → available）。
	///
	/// KEYS[1] = available 键
	/// KEYS[2] = frozen 键
	/// ARGV[1] = quantity
	/// </summary>
	private const string ReleaseScript = @"
local frozen = tonumber(redis.call('GET', KEYS[2]) or '0')
local quantity = tonumber(ARGV[1])
local actual = math.min(frozen, quantity)
if actual > 0 then
    redis.call('INCRBY', KEYS[1], actual)
    redis.call('DECRBY', KEYS[2], actual)
end
return actual";

	/// <summary>
	/// Lua 脚本：确认售出（frozen → sold）。
	///
	/// KEYS[1] = frozen 键
	/// KEYS[2] = sold 键
	/// ARGV[1] = quantity
	/// </summary>
	private const string ConfirmSoldScript = @"
local frozen = tonumber(redis.call('GET', KEYS[1]) or '0')
local quantity = tonumber(ARGV[1])
local actual = math.min(frozen, quantity)
if actual > 0 then
    redis.call('DECRBY', KEYS[1], actual)
    redis.call('INCRBY', KEYS[2], actual)
end
return actual";

	/// <inheritdoc />
	public async Task<bool> TryFreezeAsync(Guid skuId, int quantity, CancellationToken ct = default)
	{
		var db = _redisClient.GetDatabase();
		var availableKey = AvailableKey(skuId);
		var frozenKey = FrozenKey(skuId);

		var result = (long)await db.ScriptEvaluateAsync(
			FreezeScript,
			new[] { new RedisKey(availableKey), new RedisKey(frozenKey) },
			new RedisValue[] { quantity });

		if (result == 1)
		{
			_logger.LogDebug("Redis freeze succeeded. SkuId={SkuId} Quantity={Quantity}", skuId, quantity);
			return true;
		}

		_logger.LogWarning("Redis freeze failed (insufficient stock). SkuId={SkuId} Quantity={Quantity}", skuId, quantity);
		return false;
	}

	/// <inheritdoc />
	public async Task ReleaseAsync(Guid skuId, int quantity, CancellationToken ct = default)
	{
		var db = _redisClient.GetDatabase();
		var availableKey = AvailableKey(skuId);
		var frozenKey = FrozenKey(skuId);

		var actual = (long)await db.ScriptEvaluateAsync(
			ReleaseScript,
			new[] { new RedisKey(availableKey), new RedisKey(frozenKey) },
			new RedisValue[] { quantity });

		_logger.LogDebug("Redis release. SkuId={SkuId} Requested={Requested} Actual={Actual}", skuId, quantity, actual);
	}

	/// <inheritdoc />
	public async Task ConfirmSoldAsync(Guid skuId, int quantity, CancellationToken ct = default)
	{
		var db = _redisClient.GetDatabase();
		var frozenKey = FrozenKey(skuId);
		var soldKey = SoldKey(skuId);

		var actual = (long)await db.ScriptEvaluateAsync(
			ConfirmSoldScript,
			new[] { new RedisKey(frozenKey), new RedisKey(soldKey) },
			new RedisValue[] { quantity });

		_logger.LogDebug("Redis confirm sold. SkuId={SkuId} Requested={Requested} Actual={Actual}", skuId, quantity, actual);
	}

	/// <inheritdoc />
	public async Task SyncStockAsync(Guid skuId, int available, int frozen, int sold, CancellationToken ct = default)
	{
		var db = _redisClient.GetDatabase();
		var availableKey = AvailableKey(skuId);
		var frozenKey = FrozenKey(skuId);
		var soldKey = SoldKey(skuId);

		var batch = db.CreateBatch();
		var tasks = new List<Task>
		{
			batch.StringSetAsync(availableKey, available, DefaultTtl),
			batch.StringSetAsync(frozenKey, frozen, DefaultTtl),
			batch.StringSetAsync(soldKey, sold, DefaultTtl)
		};
		batch.Execute();
		await Task.WhenAll(tasks);

		_logger.LogDebug("Redis stock synced. SkuId={SkuId} Available={Available} Frozen={Frozen} Sold={Sold}",
			skuId, available, frozen, sold);
	}

	/// <inheritdoc />
	public async Task<(int Available, int Frozen, int Sold)?> GetStockAsync(Guid skuId, CancellationToken ct = default)
	{
		var db = _redisClient.GetDatabase();
		var availableKey = AvailableKey(skuId);
		var frozenKey = FrozenKey(skuId);
		var soldKey = SoldKey(skuId);

		var batch = db.CreateBatch();
		var availableTask = batch.StringGetAsync(availableKey);
		var frozenTask = batch.StringGetAsync(frozenKey);
		var soldTask = batch.StringGetAsync(soldKey);
		batch.Execute();

		await Task.WhenAll(availableTask, frozenTask, soldTask);

		if (!availableTask.Result.HasValue && !frozenTask.Result.HasValue && !soldTask.Result.HasValue)
		{
			return null;
		}

		return (
			(int)(availableTask.Result.TryParse(out int a) ? a : 0),
			(int)(frozenTask.Result.TryParse(out int f) ? f : 0),
			(int)(soldTask.Result.TryParse(out int s) ? s : 0)
		);
	}

	/// <summary>
	/// Lua 脚本：只读检查可用库存是否足够（不修改任何键）。
	///
	/// KEYS[1] = available 键
	/// ARGV[1] = quantity
	/// </summary>
	private const string CheckAvailableScript = @"
local available = tonumber(redis.call('GET', KEYS[1]) or '0')
local quantity = tonumber(ARGV[1])
if available >= quantity then
    return 1
else
    return 0
end";

	/// <inheritdoc />
	public async Task<bool> CheckAvailableAsync(Guid skuId, int quantity, CancellationToken ct = default)
	{
		var db = _redisClient.GetDatabase();
		var availableKey = AvailableKey(skuId);

		var result = (long)await db.ScriptEvaluateAsync(
			CheckAvailableScript,
			new[] { new RedisKey(availableKey) },
			new RedisValue[] { quantity });

		return result == 1;
	}

	/// <inheritdoc />
	public async Task<bool> ExistsAsync(Guid skuId, CancellationToken ct = default)
	{
		var db = _redisClient.GetDatabase();
		var availableKey = AvailableKey(skuId);
		return await db.KeyExistsAsync(availableKey);
	}

	private static string AvailableKey(Guid skuId) => $"{TenantPrefix}{KeyPrefix}:{skuId}:available";
	private static string FrozenKey(Guid skuId) => $"{TenantPrefix}{KeyPrefix}:{skuId}:frozen";
	private static string SoldKey(Guid skuId) => $"{TenantPrefix}{KeyPrefix}:{skuId}:sold";

	private static string TenantPrefix
	{
		get
		{
			var tid = TenantContext.CurrentTenantId;
			return tid.HasValue ? $"t:{tid.Value}:" : string.Empty;
		}
	}
}
