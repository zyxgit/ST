using ST.MS.Identity.Domain.Enums;

namespace ST.MS.Identity.Application.Dtos.Menu;

public sealed class MenuTreeNodeDto
{
	public Guid Id { get; set; }

	public Guid? ParentId { get; set; }

	public string Code { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public PermissionType Type { get; set; }

	public string? Path { get; set; }

	public string? MenuIcon { get; set; }

	public string? Component { get; set; }

	public bool IsLink { get; set; }

	public bool KeepAlive { get; set; }

	public bool IsHide { get; set; }

	public int Sort { get; set; }

	public List<MenuTreeNodeDto> Children { get; set; } = [];
}
