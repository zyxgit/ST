namespace ST.MS.Identity.Application.Dtos.Tenant;

/// <summary>
/// 更新租户信息
/// </summary>
public sealed class UpdateTenantInputDto
{
	/// <summary>
	/// 租户名称
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// 套餐 ID
	/// </summary>
	public string? PackageId { get; set; }

	/// <summary>
	/// 过期时间
	/// </summary>
	public DateTime? ExpireAtUtc { get; set; }
}
