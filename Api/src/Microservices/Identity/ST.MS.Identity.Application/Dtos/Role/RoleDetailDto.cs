namespace ST.MS.Identity.Application.Dtos.Role;

public sealed class RoleDetailDto
{
	public Guid Id { get; set; }

	public string Code { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public bool IsSystem { get; set; }

	public bool IsDefault { get; set; }

	public DateTime CreateTime { get; set; }

	public DateTime ModifyTime { get; set; }

	public List<Guid> PermissionIds { get; set; } = [];
}
