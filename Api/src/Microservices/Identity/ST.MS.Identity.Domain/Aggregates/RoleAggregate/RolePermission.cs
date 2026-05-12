using ST.MS.Identity.Domain.Aggregates.PermissionAggregate;

namespace ST.MS.Identity.Domain.Aggregates.RoleAggregate;

public class RolePermission : IEntity
{
	/// <summary>
	/// 角色id
	/// </summary>
	public Guid RoleId { get; set; }

	/// <summary>
	///  权限id
	/// </summary>
	public Guid PermissionId { get; set; }

	public Role Role { get; set; } = null!;

	public Permission Permission { get; set; } = null!;
}
