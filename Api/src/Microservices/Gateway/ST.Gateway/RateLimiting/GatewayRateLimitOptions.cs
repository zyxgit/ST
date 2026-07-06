using ST.Infra.Redis.RateLimiting;

namespace ST.Gateway.RateLimiting;

/// <summary>
/// Gateway 限流配置。
/// </summary>
public sealed class GatewayRateLimitOptions
{
	/// <summary>配置节名称</summary>
	public const string SectionName = "RateLimiting";

	/// <summary>是否启用限流</summary>
	public bool Enabled { get; set; } = true;

	/// <summary>限流模式：InMemory（单机）或 Redis（分布式）</summary>
	public RateLimitMode Mode { get; set; } = RateLimitMode.InMemory;

	/// <summary>默认窗口大小（秒）</summary>
	public int DefaultWindowSeconds { get; set; } = 60;

	/// <summary>默认 API 限流（请求数/窗口）</summary>
	public int DefaultApiPermitLimit { get; set; } = 120;

	/// <summary>默认 Auth 限流（请求数/窗口）</summary>
	public int DefaultAuthPermitLimit { get; set; } = 20;

	/// <summary>默认 Docs 限流（请求数/窗口）</summary>
	public int DefaultDocsPermitLimit { get; set; } = 240;

	/// <summary>自定义限流规则列表</summary>
	public List<RateLimitRule> Rules { get; set; } = [];
}

/// <summary>
/// 限流模式。
/// </summary>
public enum RateLimitMode
{
	/// <summary>进程内限流（单机）</summary>
	InMemory,

	/// <summary>Redis 分布式限流</summary>
	Redis
}
