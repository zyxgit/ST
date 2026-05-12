using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ST.Shared.Const;
using ST.Shared.Security;

namespace ST.Shared.WebApi.Authentication;

public static class ServiceCollectionExtensions
{
	private const string NoopAuthenticationScheme = "Noop";

	private sealed class NoopAccessTokenService : IAccessTokenService
	{
		public AccessTokenResult CreateToken(AccessTokenRequest request)
		{
			throw new InvalidOperationException($"IAccessTokenService 未启用：请配置 '{SettingPrefixContants.Jwt_SigningKey}'，才能生成 JWT Token。");
		}
	}

	private sealed class NoopAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		public NoopAuthenticationHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder)
			: base(options, logger, encoder)
		{
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			return Task.FromResult(AuthenticateResult.NoResult());
		}
	}

	public static IServiceCollection AddSharedUserContext(this IServiceCollection services)
	{
		services.AddHttpContextAccessor();
		services.AddScoped<IUserContext, HttpUserContext>();
		services.AddScoped<ST.Infra.Repository.Interface.ICurrentUserIdAccessor, HttpCurrentUserIdAccessor>();
		return services;
	}

	// 注册：
	// - AddAuthentication + AddAuthorization
	// - 仅当配置了 `Jwt:SigningKey` 时启用 JwtBearer（Key 常量：SettingPrefixContants.Jwt_SigningKey）
	public static IServiceCollection AddSharedJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<JwtOptions>(configuration.GetSection(SettingPrefixContants.Jwt));

		// 即使某个服务暂时不配置 JWT，也保证 DI 可解析（避免注入失败）。
		services.AddSingleton<IAccessTokenService, NoopAccessTokenService>();
		services.AddSingleton<IRefreshTokenLifetimeProvider, ConfigurationRefreshTokenLifetimeProvider>();

		services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = NoopAuthenticationScheme;
				options.DefaultChallengeScheme = NoopAuthenticationScheme;
				options.DefaultForbidScheme = NoopAuthenticationScheme;
			})
			.AddScheme<AuthenticationSchemeOptions, NoopAuthenticationHandler>(NoopAuthenticationScheme, _ => { });
		services.AddAuthorization();

		var signingKey = configuration[SettingPrefixContants.Jwt_SigningKey];
		if (string.IsNullOrWhiteSpace(signingKey))
		{
			// 管道仍可运行；但不会启用 JwtBearer 验证，且 Token 生成会抛异常。
			return services;
		}

		services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();

		services
			.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				var issuer = configuration[SettingPrefixContants.Jwt_Issuer] ?? "st";
				var audience = configuration[SettingPrefixContants.Jwt_Audience] ?? "st";
				var keyBytes = Encoding.UTF8.GetBytes(signingKey);

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
					ValidIssuer = issuer,
					ValidateAudience = !string.IsNullOrWhiteSpace(audience),
					ValidAudience = audience,
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
					ValidateLifetime = true,
					ClockSkew = TimeSpan.FromSeconds(30),
					RoleClaimType = JwtClaimConstants.Role
				};
			});

		// 权限策略：`[Authorize(Policy = "perm:user:create")]`
		services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
		services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

		return services;
	}
}

