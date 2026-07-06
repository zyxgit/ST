namespace ST.MS.Identity.Domain.Aggregates.TenantAggregate;

/// <summary>
/// 租户用户关联
/// </summary>
public class TenantUser : IEntity
{
	/// <summary>
	/// 租户 ID
	/// </summary>
	public Guid TenantId { get; set; }

	/// <summary>
	/// 用户 ID
	/// </summary>
	public Guid UserId { get; set; }

	/// <summary>
	/// 租户内角色（owner / admin / member）
	/// </summary>
	public string? RoleInTenant { get; set; }

	/// <summary>
	/// 加入时间
	/// </summary>
	public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

	/// <summary>
	/// 导航属性：租户
	/// </summary>
	public Tenant Tenant { get; set; } = null!;
}
