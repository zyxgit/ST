namespace ST.MS.Identity.Application.Dtos.Tenant;

/// <summary>
/// 添加租户用户
/// </summary>
public sealed class AddTenantUserInputDto
{
	/// <summary>
	/// 用户 ID
	/// </summary>
	public Guid UserId { get; set; }

	/// <summary>
	/// 租户内角色（owner / admin / member）
	/// </summary>
	public string? RoleInTenant { get; set; }
}
