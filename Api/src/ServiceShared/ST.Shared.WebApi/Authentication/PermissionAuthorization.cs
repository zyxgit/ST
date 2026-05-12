using Microsoft.AspNetCore.Authorization;

namespace ST.Shared.WebApi.Authentication;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
	public PermissionRequirement(string permission)
	{
		Permission = permission;
	}

	public string Permission { get; }
}

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
	{
		if (context.User?.Identity?.IsAuthenticated != true)
		{
			return Task.CompletedTask;
		}

		var has = context.User.Claims.Any(c =>
			c.Type == JwtClaimConstants.Permission
			&& string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

		if (has)
		{
			context.Succeed(requirement);
		}

		return Task.CompletedTask;
	}
}

// Policy 命名约定：`perm:<permission-code>`
public sealed class PermissionAuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
	private const string Prefix = "perm:";
	private readonly DefaultAuthorizationPolicyProvider _fallback;

	public PermissionAuthorizationPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options)
	{
		_fallback = new DefaultAuthorizationPolicyProvider(options);
	}

	public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

	public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

	public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
	{
		if (!policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
		{
			return _fallback.GetPolicyAsync(policyName);
		}

		var permission = policyName.Substring(Prefix.Length).Trim();
		if (string.IsNullOrWhiteSpace(permission))
		{
			return Task.FromResult<AuthorizationPolicy?>(null);
		}

		var policy = new AuthorizationPolicyBuilder()
			.AddRequirements(new PermissionRequirement(permission))
			.Build();

		return Task.FromResult<AuthorizationPolicy?>(policy);
	}
}
