using System.Security.Claims;

namespace ST.Shared.WebApi.Authentication;

public static class JwtClaimConstants
{
	public const string Permission = "perm";
	public const string NickName = "nickname";

	// Keep roles compatible with ASP.NET Core authorization:
	public const string Role = ClaimTypes.Role;

	/// <summary>
	/// 租户 ID（JWT claim key: "tid"）
	/// </summary>
	public const string TenantId = "tid";

	/// <summary>
	/// 租户编码（JWT claim key: "tcode"）
	/// </summary>
	public const string TenantCode = "tcode";
}

