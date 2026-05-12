using System.Security.Claims;

namespace ST.Shared.WebApi.Authentication;

public static class JwtClaimConstants
{
	public const string Permission = "perm";
	public const string NickName = "nickname";

	// Keep roles compatible with ASP.NET Core authorization:
	public const string Role = ClaimTypes.Role;
}

