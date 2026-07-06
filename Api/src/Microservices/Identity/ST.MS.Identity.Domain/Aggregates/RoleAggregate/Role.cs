namespace ST.MS.Identity.Domain.Aggregates.RoleAggregate;

/// <summary>
/// 角色信息
/// </summary>
public class Role : AggregateRoot, ISoftDelete
{
	public Role() { }

	public Role(string name, string code, string description, bool isSystem, bool isDefault)
	{
		Code = code;
		Name = name;
		Description = description;
		IsSystem = isSystem;
		IsDefault = isDefault;
	}

	/// <summary>
	/// 角色编码
	/// </summary>
	public string Code { get; set; } = null!;

	/// <summary>
	/// 角色名称
	/// </summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// 角色描述
	/// </summary>
	public string Description { get; set; } = null!;

	/// <summary>
	/// 是否系统角色
	/// </summary>
	public bool IsSystem { get; private set; }

	/// <summary>
	/// 是否默认角色
	/// </summary>
	public bool IsDefault { get; private set; }

	/// <summary>
	/// 是否删除
	/// </summary>
	public bool IsDeleted { get; set; }

	/// <summary>
	/// 角色权限
	/// </summary>
	public List<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

	#region 行为

	public void SetCode(string code) => Code = code;

	public void SetName(string name) => Name = name;

	public void SetDescription(string description) => Description = description;

	public void SetIsSystem(bool isSystem) => IsSystem = isSystem;

	public void SetIsDefault(bool isDefault) => IsDefault = isDefault;


	#endregion
}
