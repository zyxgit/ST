namespace ST.MS.Identity.Application.Dtos.User;

public sealed class UpdateUserInputDto
{
	public string NickName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string? Phone { get; set; }

	public bool IsEnable { get; set; } = true;

	public List<Guid> RoleIds { get; set; } = [];
}
