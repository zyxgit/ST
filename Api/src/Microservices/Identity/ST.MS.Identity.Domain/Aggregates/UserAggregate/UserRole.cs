using ST.MS.Identity.Domain.Aggregates.RoleAggregate;

namespace ST.MS.Identity.Domain.Aggregates.UserAggregate;

/// <summary>
/// 用户角色
/// </summary>
public class UserRole : IEntity
{
	/// <summary>
	/// 用户Id
	/// </summary>
	public Guid UserId { get; set; }

	/// <summary>
	/// 角色Id
	/// </summary>
	public Guid RoleId { get; set; }

	public User User { get; set; } = null!;

	public Role Role { get; set; } = null!;
}
