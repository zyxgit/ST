namespace ST.MS.Identity.Application.Dtos.User;

public sealed class UserListItemDto
{
	public Guid Id { get; init; }

	public string NickName { get; init; } = string.Empty;

	public string Email { get; init; } = string.Empty;

	public string Phone { get; init; } = string.Empty;

	public bool IsEnable { get; init; }

	public DateTime CreateTime { get; init; }

	public DateTime ModifyTime { get; init; }

	public DateTime? LastLoginTime { get; init; }

	public string? LastLoginIp { get; init; }

	public Guid? AvatarFileId { get; init; }

	public IReadOnlyList<string> Roles { get; init; } = [];
}
