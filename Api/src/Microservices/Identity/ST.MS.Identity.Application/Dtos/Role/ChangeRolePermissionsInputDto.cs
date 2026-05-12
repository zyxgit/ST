namespace ST.MS.Identity.Application.Dtos.Role;

public sealed class ChangeRolePermissionsInputDto
{
	public List<Guid> PermissionIds { get; set; } = [];
}
