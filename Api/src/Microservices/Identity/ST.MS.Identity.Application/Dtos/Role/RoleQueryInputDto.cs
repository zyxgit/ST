using ST.Shared.Application.Dtos;

namespace ST.MS.Identity.Application.Dtos.Role;

public sealed class RoleQueryInputDto : PagedRequestDto
{
	public string? Keyword { get; set; }

	public bool? IsSystem { get; set; }

	public bool? IsDefault { get; set; }
}
