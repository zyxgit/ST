namespace ST.MS.Identity.Application.Dtos.User;

public sealed class CreateUserInputDto
{
	public string NickName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public string? Phone { get; set; }

	public string Password { get; set; } = string.Empty;

	public bool IsEnable { get; set; } = true;

	public List<Guid> RoleIds { get; set; } = [];
}
