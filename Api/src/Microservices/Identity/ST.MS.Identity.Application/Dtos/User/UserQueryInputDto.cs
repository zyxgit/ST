using ST.Shared.Application.Dtos;

namespace ST.MS.Identity.Application.Dtos.User;

public sealed class UserQueryInputDto : PagedRequestDto
{
	public string? Keyword { get; set; }

	public bool? IsEnable { get; set; }

	public Guid? RoleId { get; set; }
}
