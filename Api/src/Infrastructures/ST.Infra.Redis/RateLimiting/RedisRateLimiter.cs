using ST.Infra.Redis.Provider;

namespace ST.Infra.Redis.RateLimiting;

/// <summary>
/// 基于 Redis Lua 脚本的分布式限流实现。
/// 使用滑动窗口算法（Sorted Set），保证原子性。
/// </summary>
public sealed class RedisRateLimiter : IRateLimiter
{
	private readonly IRedisClient _redisClient;

	/// <summary>
	/// Lua 脚本：滑动窗口限流。
	/// KEYS[1] = 限流键
	/// ARGV[1] = 窗口大小（毫秒）
	/// ARGV[2] = 最大请求数
	/// ARGV[3] = 当前时间戳（毫秒）
	/// ARGV[4] = 唯一请求标识
	///
	/// 返回：{ allowed (1/0), currentCount }
	/// </summary>
	private const string SlidingWindowScript = @"
local key = KEYS[1]
local window_ms = tonumber(ARGV[1])
local limit = tonumber(ARGV[2])
local now = tonumber(ARGV[3])
local member = ARGV[4]

-- 清除窗口外的过期成员
redis.call('ZREMRANGEBYSCORE', key, 0, now - window_ms)

-- 获取当前窗口内的请求数
local current = redis.call('ZCARD', key)

if current < limit then
    -- 未超限，添加当前请求
    redis.call('ZADD', key, now, member)
    redis.call('PEXPIRE', key, window_ms)
    return {1, current + 1}
else
    -- 已超限，返回当前计数
    return {0, current}
end";

	public RedisRateLimiter(IRedisClient redisClient)
	{
		_redisClient = redisClient;
	}

	/// <inheritdoc />
	public async Task<RateLimitResult> CheckAsync(RateLimitRule rule, string partitionKey, CancellationToken ct = default)
	{
		var db = _redisClient.GetDatabase();
		var key = BuildKey(rule, partitionKey);
		var windowMs = rule.WindowSeconds * 1000L;
		var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		var member = $"{now}:{Guid.NewGuid():N}";

		var result = await db.ScriptEvaluateAsync(
			SlidingWindowScript,
			new[] { new RedisKey(key) },
			new RedisValue[]
			{
				windowMs,
				rule.PermitLimit,
				now,
				member
			});

		var values = (long[])result!;
		var allowed = values[0] == 1;
		var currentCount = values[1];

		if (allowed)
		{
			return RateLimitResult.Allowed(currentCount, rule.PermitLimit);
		}

		// 计算重试时间：获取窗口内最早的请求时间
		var earliest = await GetEarliestScoreAsync(key);
		var retryAfterMs = earliest > 0
			? Math.Max(0, (earliest + windowMs) - now)
			: windowMs;

		return RateLimitResult.Rejected(
			currentCount,
			rule.PermitLimit,
			TimeSpan.FromMilliseconds(retryAfterMs));
	}

	/// <summary>
	/// 构建 Redis 键。
	/// 格式：rate:{ruleName}:{partitionKey}
	/// </summary>
	private static string BuildKey(RateLimitRule rule, string partitionKey)
	{
		return $"rate:{rule.Name}:{partitionKey}";
	}

	/// <summary>
	/// 获取有序集合中最早的分数（时间戳）。
	/// </summary>
	private async Task<long> GetEarliestScoreAsync(string key)
	{
		var db = _redisClient.GetDatabase();
		var result = await db.SortedSetRangeByScoreWithScoresAsync(key, take: 1);
		return result.Length > 0 ? (long)result[0].Score : 0;
	}
}
