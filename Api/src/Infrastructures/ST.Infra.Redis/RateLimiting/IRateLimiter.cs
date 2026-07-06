namespace ST.Infra.Redis.RateLimiting;

/// <summary>
/// 限流服务接口。
/// </summary>
public interface IRateLimiter
{
	/// <summary>
	/// 检查请求是否允许通过。
	/// </summary>
	/// <param name="rule">限流规则</param>
	/// <param name="partitionKey">分区键（如 IP、用户 ID、路径等）</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>限流结果</returns>
	Task<RateLimitResult> CheckAsync(RateLimitRule rule, string partitionKey, CancellationToken ct = default);
}

/// <summary>
/// 限流结果。
/// </summary>
public sealed class RateLimitResult
{
	/// <summary>是否允许通过</summary>
	public bool IsAllowed { get; init; }

	/// <summary>当前窗口已请求数</summary>
	public long CurrentCount { get; init; }

	/// <summary>窗口内允许的最大请求数</summary>
	public int PermitLimit { get; init; }

	/// <summary>重试等待时间（被拒绝时）</summary>
	public TimeSpan? RetryAfter { get; init; }

	/// <summary>
	/// 创建允许通过的结果。
	/// </summary>
	public static RateLimitResult Allowed(long currentCount, int permitLimit) => new()
	{
		IsAllowed = true,
		CurrentCount = currentCount,
		PermitLimit = permitLimit
	};

	/// <summary>
	/// 创建被拒绝的结果。
	/// </summary>
	public static RateLimitResult Rejected(long currentCount, int permitLimit, TimeSpan retryAfter) => new()
	{
		IsAllowed = false,
		CurrentCount = currentCount,
		PermitLimit = permitLimit,
		RetryAfter = retryAfter
	};
}
