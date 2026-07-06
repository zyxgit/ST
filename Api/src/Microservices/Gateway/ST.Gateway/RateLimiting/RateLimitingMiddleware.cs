using System.Globalization;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using ST.Infra.Redis.RateLimiting;

namespace ST.Gateway.RateLimiting;

/// <summary>
/// Gateway 限流中间件。
/// 支持 InMemory（单机）和 Redis（分布式）两种模式。
/// 支持多规则匹配，按路径前缀、HTTP 方法等维度限流。
/// </summary>
public sealed class RateLimitingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly GatewayRateLimitOptions _options;
	private readonly IRateLimiter? _redisRateLimiter;
	private readonly ILogger<RateLimitingMiddleware> _logger;

	public RateLimitingMiddleware(
		RequestDelegate next,
		IOptions<GatewayRateLimitOptions> options,
		IRateLimiter? redisRateLimiter,
		ILogger<RateLimitingMiddleware> logger)
	{
		_next = next;
		_options = options.Value;
		_redisRateLimiter = redisRateLimiter;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		if (!_options.Enabled)
		{
			await _next(context);
			return;
		}

		// 匹配规则
		var rule = MatchRule(context.Request);
		if (rule is null)
		{
			// 无匹配规则，使用默认配置
			rule = CreateDefaultRule(context.Request);
		}

		// 构建分区键
		var partitionKey = BuildPartitionKey(rule, context);

		// 执行限流检查（带 200ms 超时，超时直接放行）
		RateLimitResult result;
		try
		{
			if (_options.Mode == RateLimitMode.Redis && _redisRateLimiter is not null)
			{
				using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
				result = await _redisRateLimiter.CheckAsync(rule, partitionKey, cts.Token);
			}
			else
			{
				// InMemory 模式或 Redis 不可用时，直接放行
				result = RateLimitResult.Allowed(0, rule.PermitLimit);
			}
		}
		catch (OperationCanceledException)
		{
			// Redis 超时，降级放行
			_logger.LogWarning("Redis rate limiting timed out for {PartitionKey}, falling back to allow", partitionKey);
			result = RateLimitResult.Allowed(0, rule.PermitLimit);
		}
		catch (Exception ex)
		{
			// Redis 连接失败时降级放行，记录警告
			_logger.LogWarning(ex, "Redis rate limiting failed for {PartitionKey}, falling back to allow", partitionKey);
			result = RateLimitResult.Allowed(0, rule.PermitLimit);
		}

		if (!result.IsAllowed)
		{
			_logger.LogWarning("Rate limit exceeded for {PartitionKey}, rule: {RuleName}",
				partitionKey, rule.Name);

			context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
			if (result.RetryAfter.HasValue)
			{
				var seconds = Math.Max(1, (int)Math.Ceiling(result.RetryAfter.Value.TotalSeconds));
				context.Response.Headers["Retry-After"] = seconds.ToString(CultureInfo.InvariantCulture);
			}

			await context.Response.WriteAsJsonAsync(new
			{
				type = "https://tools.ietf.org/html/rfc7807",
				title = "Too Many Requests",
				status = 429,
				detail = $"Rate limit exceeded. Try again in {result.RetryAfter?.TotalSeconds ?? 60} seconds."
			});
			return;
		}

		await _next(context);
	}

	/// <summary>
	/// 匹配限流规则。
	/// </summary>
	private RateLimitRule? MatchRule(HttpRequest request)
	{
		var path = request.Path.Value ?? string.Empty;
		var method = request.Method;

		foreach (var rule in _options.Rules)
		{
			// 路径前缀匹配
			if (!string.IsNullOrEmpty(rule.PathPrefix) &&
				!path.StartsWith(rule.PathPrefix, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			// HTTP 方法匹配
			if (!string.IsNullOrEmpty(rule.HttpMethod) &&
				!string.Equals(rule.HttpMethod, method, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			return rule;
		}

		return null;
	}

	/// <summary>
	/// 创建默认规则。
	/// </summary>
	private RateLimitRule CreateDefaultRule(HttpRequest request)
	{
		var path = request.Path.Value ?? string.Empty;
		var isAuth = path.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
					 path.Contains("/register", StringComparison.OrdinalIgnoreCase) ||
					 path.Contains("/refresh", StringComparison.OrdinalIgnoreCase);
		var isDocs = path.StartsWith("/docs", StringComparison.OrdinalIgnoreCase) ||
					 path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
					 path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase);

		var permitLimit = isAuth ? _options.DefaultAuthPermitLimit
			: isDocs ? _options.DefaultDocsPermitLimit
			: _options.DefaultApiPermitLimit;

		return new RateLimitRule
		{
			Name = isAuth ? "auth-default" : isDocs ? "docs-default" : "api-default",
			PermitLimit = permitLimit,
			WindowSeconds = _options.DefaultWindowSeconds,
			PartitionBy = RateLimitPartitionBy.Ip
		};
	}

	/// <summary>
	/// 构建分区键。
	/// </summary>
	private static string BuildPartitionKey(RateLimitRule rule, HttpContext context)
	{
		var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
					 context.User.FindFirstValue("sub");
		var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
		var path = context.Request.Path.Value ?? string.Empty;
		var tenantId = context.User.FindFirstValue("tid") ?? "anonymous";

		return rule.PartitionBy switch
		{
			RateLimitPartitionBy.Ip => $"ip:{ip}",
			RateLimitPartitionBy.User => $"user:{userId ?? ip}",
			RateLimitPartitionBy.Path => $"path:{path}",
			RateLimitPartitionBy.IpPath => $"ip:{ip}:path:{path}",
			RateLimitPartitionBy.UserPath => $"user:{userId ?? ip}:path:{path}",
			RateLimitPartitionBy.Tenant => $"tenant:{tenantId}",
			RateLimitPartitionBy.TenantUser => $"tenant:{tenantId}:user:{userId ?? ip}",
			RateLimitPartitionBy.TenantPath => $"tenant:{tenantId}:path:{path}",
			_ => $"ip:{ip}"
		};
	}
}
