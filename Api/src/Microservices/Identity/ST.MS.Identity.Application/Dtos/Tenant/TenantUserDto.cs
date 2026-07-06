namespace ST.MS.Identity.Application.Dtos.Tenant;

/// <summary>
/// 租户用户信息
/// </summary>
public sealed class TenantUserDto
{
	public Guid UserId { get; set; }

	/// <summary>
	/// 用户昵称
	/// </summary>
	public string NickName { get; set; } = string.Empty;

	/// <summary>
	/// 用户邮箱
	/// </summary>
	public string Email { get; set; } = string.Empty;

	/// <summary>
	/// 租户内角色
	/// </summary>
	public string? RoleInTenant { get; set; }

	/// <summary>
	/// 加入时间
	/// </summary>
	public DateTime JoinedAtUtc { get; set; }
}
