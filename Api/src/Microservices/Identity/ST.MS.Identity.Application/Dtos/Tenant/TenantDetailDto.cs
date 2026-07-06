namespace ST.MS.Identity.Application.Dtos.Tenant;

/// <summary>
/// 租户详情
/// </summary>
public sealed class TenantDetailDto
{
	public Guid Id { get; set; }

	public string Code { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 状态：Active / Suspended / Deleted
	/// </summary>
	public string Status { get; set; } = string.Empty;

	public string? PackageId { get; set; }

	public DateTime? ExpireAtUtc { get; set; }

	/// <summary>
	/// 关联用户数
	/// </summary>
	public int UserCount { get; set; }

	public DateTime CreateTime { get; set; }

	public DateTime ModifyTime { get; set; }

	/// <summary>
	/// 租户配额（可选，可能尚未设置）
	/// </summary>
	public TenantQuotaDto? Quota { get; set; }
}
