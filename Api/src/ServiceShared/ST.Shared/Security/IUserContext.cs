namespace ST.Shared.Security;

public interface IUserContext
{
	bool IsAuthenticated { get; }

	Guid? UserId { get; }

	string? Email { get; }

	string? NickName { get; }

	/// <summary>
	/// 客户端 IP（优先 X-Forwarded-For，否则 RemoteIpAddress）
	/// </summary>
	string? ClientIp { get; }

	IReadOnlyList<string> Roles { get; }

	IReadOnlyList<string> Permissions { get; }
}
