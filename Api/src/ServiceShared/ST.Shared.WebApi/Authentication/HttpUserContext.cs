using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ST.Shared.Security;

namespace ST.Shared.WebApi.Authentication;

public sealed class HttpUserContext : IUserContext
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public HttpUserContext(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;

	public string? ClientIp
	{
		get
		{
			var httpContext = _httpContextAccessor.HttpContext;
			if (httpContext == null)
			{
				return null;
			}

			// 常见反向代理头：X-Forwarded-For: client, proxy1, proxy2
			if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
			{
				var first = forwardedFor.ToString()
					.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
					.FirstOrDefault();
				if (!string.IsNullOrWhiteSpace(first))
				{
					return first;
				}
			}

			return httpContext.Connection.RemoteIpAddress?.ToString();
		}
	}

	public Guid? UserId
	{
		get
		{
			var sub = FindFirstValue(JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);
			return Guid.TryParse(sub, out var id) ? id : null;
		}
	}

	public string? Email => FindFirstValue(JwtRegisteredClaimNames.Email, ClaimTypes.Email);

	public string? NickName => _httpContextAccessor.HttpContext?.User?.FindFirst(JwtClaimConstants.NickName)?.Value;

	public IReadOnlyList<string> Roles =>
		_httpContextAccessor.HttpContext?.User?.Claims
			.Where(c => c.Type == JwtClaimConstants.Role)
			.Select(c => c.Value)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList()
		?? [];

	public IReadOnlyList<string> Permissions =>
		_httpContextAccessor.HttpContext?.User?.Claims
			.Where(c => c.Type == JwtClaimConstants.Permission)
			.Select(c => c.Value)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList()
		?? [];

	private string? FindFirstValue(params string[] claimTypes)
	{
		var user = _httpContextAccessor.HttpContext?.User;
		if (user is null)
		{
			return null;
		}

		foreach (var claimType in claimTypes)
		{
			var value = user.FindFirst(claimType)?.Value;
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value;
			}
		}

		return null;
	}
}
