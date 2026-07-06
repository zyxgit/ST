namespace ST.MS.Identity.Application.Dtos.Tenant;

/// <summary>
/// 租户列表项
/// </summary>
public sealed class TenantListItemDto
{
	public Guid Id { get; set; }

	public string Code { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public string? PackageId { get; set; }

	public DateTime? ExpireAtUtc { get; set; }

	/// <summary>
	/// 关联用户数
	/// </summary>
	public int UserCount { get; set; }

	public DateTime CreateTime { get; set; }
}
