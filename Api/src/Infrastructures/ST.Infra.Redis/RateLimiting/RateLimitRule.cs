namespace ST.Infra.Redis.RateLimiting;

/// <summary>
/// 限流规则配置。
/// </summary>
public sealed class RateLimitRule
{
	/// <summary>规则名称（如 "auth-login", "api-default"）</summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>路径前缀匹配（如 "/api/identity/user/login"）</summary>
	public string? PathPrefix { get; set; }

	/// <summary>窗口内允许的最大请求数</summary>
	public int PermitLimit { get; set; } = 100;

	/// <summary>限流窗口大小（秒）</summary>
	public int WindowSeconds { get; set; } = 60;

	/// <summary>分区维度（Ip, User, Path, IpPath, UserPath）</summary>
	public RateLimitPartitionBy PartitionBy { get; set; } = RateLimitPartitionBy.Ip;

	/// <summary>HTTP 方法过滤（null 表示不限制）</summary>
	public string? HttpMethod { get; set; }
}

/// <summary>
/// 限流分区维度。
/// </summary>
public enum RateLimitPartitionBy
{
	/// <summary>按 IP 分区</summary>
	Ip,

	/// <summary>按用户 ID 分区</summary>
	User,

	/// <summary>按请求路径分区</summary>
	Path,

	/// <summary>按 IP + 路径分区</summary>
	IpPath,

	/// <summary>按用户 + 路径分区</summary>
	UserPath,

	/// <summary>按租户分区</summary>
	Tenant,

	/// <summary>按租户 + 用户分区</summary>
	TenantUser,

	/// <summary>按租户 + 路径分区</summary>
	TenantPath
}
