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

	/// <summary>
	/// 当前租户 ID（来自 JWT claim "tid"）
	/// </summary>
	Guid? TenantId { get; }

	/// <summary>
	/// 当前租户编码（来自 JWT claim "tcode"）
	/// </summary>
	string? TenantCode { get; }
}
