using Microsoft.Extensions.Options;

namespace ST.Gateway.RateLimiting;

/// <summary>
/// 限流中间件扩展方法。
/// </summary>
public static class RateLimitingExtensions
{
	/// <summary>
	/// 添加 Gateway 限流配置。
	/// </summary>
	public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
	{
		services.Configure<GatewayRateLimitOptions>(configuration.GetSection(GatewayRateLimitOptions.SectionName));
		return services;
	}

	/// <summary>
	/// 使用 Gateway 限流中间件。
	/// </summary>
	public static IApplicationBuilder UseGatewayRateLimiting(this IApplicationBuilder app)
	{
		return app.UseMiddleware<RateLimitingMiddleware>();
	}
}
