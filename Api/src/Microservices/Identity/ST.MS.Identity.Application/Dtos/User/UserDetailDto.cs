namespace ST.MS.Identity.Application.Dtos.User;

public sealed class UserDetailDto
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

	/// <summary>
	/// 锁定原因
	/// </summary>
	public string? LockReason { get; init; }

	/// <summary>
	/// 锁定时间
	/// </summary>
	public DateTime? LockedAtUtc { get; init; }

	public IReadOnlyList<RoleOptionDto> Roles { get; init; } = [];
}
