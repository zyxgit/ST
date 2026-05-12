namespace ST.MS.Identity.Application.Dtos.User;

public sealed class LoginResultDto
{
	public string AccessToken { get; set; } = string.Empty;

	public DateTimeOffset ExpiresAt { get; set; }

	public string RefreshToken { get; set; } = string.Empty;

	public DateTimeOffset RefreshTokenExpiresAt { get; set; }
}
