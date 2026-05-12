namespace ST.MS.Identity.Application.Dtos.User;

public sealed class RoleOptionDto
{
	public Guid Id { get; init; }

	public string Code { get; init; } = string.Empty;

	public string Name { get; init; } = string.Empty;
}
