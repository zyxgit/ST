namespace ST.Shared.Security;

public sealed record AccessTokenRequest(
	Guid UserId,
	string Email,
	string? NickName,
	IReadOnlyCollection<string> Roles,
	IReadOnlyCollection<string> Permissions
);

public sealed record AccessTokenResult(
	string AccessToken,
	DateTimeOffset ExpiresAt
);

public interface IAccessTokenService
{
	AccessTokenResult CreateToken(AccessTokenRequest request);
}

