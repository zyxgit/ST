using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using ST.Gateway.RateLimiting;
using ST.Infra.Redis.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Gateway 作为纯代理，不使用 AddServiceDefaults()（含 OpenTelemetry/ServiceDiscovery/Resilience），
// 这些会给每个代理请求增加 1.5s+ 开销。仅保留健康检查端点。
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), ["live"]);

ApplyGatewayDestinationOverrides(builder.Configuration);
ConfigureForwardedHeaders(builder.Services, builder.Configuration);

ConfigureCors(builder.Services, builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddOpenApi();

// Gateway 分布式限流
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddRedisInfra(builder.Configuration);
builder.Services.AddRedisRateLimiting();

var rateLimiterEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", true);
if (rateLimiterEnabled)
{
	var apiPermitLimit = builder.Configuration.GetValue("RateLimiting:ApiPermitLimit", 120L);
	var authPermitLimit = builder.Configuration.GetValue("RateLimiting:AuthPermitLimit", 20L);
	var docsPermitLimit = builder.Configuration.GetValue("RateLimiting:DocsPermitLimit", 240L);
	var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);
	var window = TimeSpan.FromSeconds(windowSeconds);

	builder.Services.AddRateLimiter(options =>
	{
		options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
		options.OnRejected = static (context, _) =>
		{
			if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
			{
				var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
				context.HttpContext.Response.Headers["Retry-After"] = seconds.ToString(CultureInfo.InvariantCulture);
			}

			return ValueTask.CompletedTask;
		};

		options.AddPolicy("gateway-proxy", context =>
		{
			var scope = ResolveRequestScope(context.Request.Path);
			var bucket = ResolveRequestBucket(context.Request.Path);
			var permitLimit = bucket switch
			{
				GatewayRequestBucket.Auth => authPermitLimit,
				GatewayRequestBucket.Docs => docsPermitLimit,
				_ => apiPermitLimit
			};

			var partitionKey = $"{scope}:{bucket}:{ResolveCallerKey(context)}";
			return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = (int)Math.Clamp(permitLimit, 1, int.MaxValue),
				Window = window,
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = 0,
				AutoReplenishment = true
			});
		});

		options.AddPolicy("gateway-local-docs", context =>
		{
			var partitionKey = $"gateway-docs:{ResolveCallerKey(context)}";
			return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = (int)Math.Clamp(docsPermitLimit, 1, int.MaxValue),
				Window = window,
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = 0,
				AutoReplenishment = true
			});
		});
	});
}

builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/alive", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});

if (builder.Configuration.GetValue("ForwardedHeaders:Enabled", true))
{
	app.UseForwardedHeaders();
}

// ── CorrelationId 中间件 ─────────────────────────────────────────────────────
// 读取请求头 X-Correlation-Id，若无则从 traceparent 或 TraceId 生成；
// 存入 HttpContext.Items 并写入响应头，YARP 转发时自动携带。
app.Use(async (context, next) =>
{
	const string headerName = "X-Correlation-Id";

	// 优先从请求头读取
	var correlationId = context.Request.Headers[headerName].FirstOrDefault();

	// 若无，从 W3C traceparent 提取 TraceId
	if (string.IsNullOrWhiteSpace(correlationId))
	{
		var traceParent = context.Request.Headers["traceparent"].FirstOrDefault();
		if (!string.IsNullOrWhiteSpace(traceParent) && traceParent.Length >= 32)
		{
			// traceparent 格式: {version}-{trace-id}-{parent-id}-{flags}
			correlationId = traceParent.Split('-')[1];
		}
	}

	// 若仍无，使用当前 Activity 的 TraceId 或生成新的
	if (string.IsNullOrWhiteSpace(correlationId))
	{
		correlationId = System.Diagnostics.Activity.Current?.TraceId.ToString()
			?? Guid.NewGuid().ToString("N");
	}

	context.Items["CorrelationId"] = correlationId;
	context.Response.Headers[headerName] = correlationId;

	await next();
});

app.UseCors("st-default");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();

if (rateLimiterEnabled)
{
	app.UseRateLimiter();
	app.UseGatewayRateLimiting();
}

var rootRedirect = app.MapGet("/", () => Results.Redirect("/docs")).ExcludeFromDescription();
var docsRedirect = app.MapGet("/docs", () => Results.Redirect("/docs/index.html")).ExcludeFromDescription();
var identityDocsRedirect = app.MapGet("/docs/identity", () => Results.Redirect("/docs/identity/scalar/v1")).ExcludeFromDescription();
var operationLogDocsRedirect = app.MapGet("/docs/operationlog", () => Results.Redirect("/docs/operationlog/scalar/v1")).ExcludeFromDescription();
var testDocsRedirect = app.MapGet("/docs/test", () => Results.Redirect("/docs/test/scalar/v1")).ExcludeFromDescription();
var fileuploadDocsRedirect = app.MapGet("/docs/fileupload", () => Results.Redirect("/docs/fileupload/scalar/v1")).ExcludeFromDescription();

if (rateLimiterEnabled)
{
	rootRedirect.RequireRateLimiting("gateway-local-docs");
	docsRedirect.RequireRateLimiting("gateway-local-docs");
	identityDocsRedirect.RequireRateLimiting("gateway-local-docs");
	operationLogDocsRedirect.RequireRateLimiting("gateway-local-docs");
	testDocsRedirect.RequireRateLimiting("gateway-local-docs");
	fileuploadDocsRedirect.RequireRateLimiting("gateway-local-docs");
}

var reverseProxy = app.MapReverseProxy();

if (rateLimiterEnabled)
{
	reverseProxy.RequireRateLimiting("gateway-proxy");
}

if (app.Environment.IsDevelopment())
{
	var openApi = app.MapOpenApi();
	var scalar = app.MapScalarApiReference(options =>
	{
		options.DefaultFonts = true;
		options.Theme = ScalarTheme.Kepler;
		options.Layout = ScalarLayout.Classic;
	});

	if (rateLimiterEnabled)
	{
		openApi.RequireRateLimiting("gateway-local-docs");
		scalar.RequireRateLimiting("gateway-local-docs");
	}
}

app.Run();

static void ApplyGatewayDestinationOverrides(ConfigurationManager configuration)
{
	var mapping = new Dictionary<string, string?>
	{
		["ReverseProxy:Clusters:identity-cluster:Destinations:identity-destination:Address"] = configuration["DownstreamServices:Identity:Address"],
		["ReverseProxy:Clusters:operationlog-cluster:Destinations:operationlog-destination:Address"] = configuration["DownstreamServices:OperationLog:Address"],
		["ReverseProxy:Clusters:test-cluster:Destinations:test-destination:Address"] = configuration["DownstreamServices:Test:Address"],
		["ReverseProxy:Clusters:fileupload-cluster:Destinations:fileupload-destination:Address"] = configuration["DownstreamServices:FileUpload:Address"]
	};

	configuration.AddInMemoryCollection(mapping.Where(x => !string.IsNullOrWhiteSpace(x.Value))!);
}

static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
{
	services.AddCors(options =>
	{
		options.AddPolicy("st-default", policy =>
		{
			var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

			if (allowedOrigins.Length > 0)
			{
				policy.WithOrigins(allowedOrigins)
					.AllowAnyHeader()
					.AllowAnyMethod()
					.AllowCredentials();
			}
			else if (string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Development", StringComparison.OrdinalIgnoreCase))
			{
				policy.AllowAnyOrigin()
					.AllowAnyHeader()
					.AllowAnyMethod();
			}
			else
			{
				policy.SetIsOriginAllowed(_ => false);
			}
		});
	});
}

static void ConfigureForwardedHeaders(IServiceCollection services, IConfiguration configuration)
{
	if (!configuration.GetValue("ForwardedHeaders:Enabled", true))
	{
		return;
	}

	var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
	var trustAll = configuration.GetValue("ForwardedHeaders:TrustAll", false);
	var forwardLimit = configuration.GetValue("ForwardedHeaders:ForwardLimit", 1);

	services.Configure<ForwardedHeadersOptions>(options =>
	{
		options.ForwardedHeaders =
			ForwardedHeaders.XForwardedFor |
			ForwardedHeaders.XForwardedProto |
			ForwardedHeaders.XForwardedHost;
		options.ForwardLimit = Math.Max(1, forwardLimit);

		if (trustAll)
		{
			options.KnownIPNetworks.Clear();
			options.KnownProxies.Clear();
			return;
		}

		options.KnownProxies.Add(IPAddress.Loopback);
		options.KnownProxies.Add(IPAddress.IPv6Loopback);

		foreach (var proxy in knownProxies)
		{
			if (IPAddress.TryParse(proxy, out var address))
			{
				options.KnownProxies.Add(address);
			}
		}
	});
}

static string ResolveCallerKey(HttpContext context)
{
	var userId =
		context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
		context.User.FindFirstValue("sub");

	if (!string.IsNullOrWhiteSpace(userId))
	{
		return $"user:{userId}";
	}

	var remoteIp = context.Connection.RemoteIpAddress?.ToString();
	if (!string.IsNullOrWhiteSpace(remoteIp))
	{
		return $"ip:{remoteIp}";
	}

	return "anonymous";
}

static string ResolveRequestScope(PathString path)
{
	var pathValue = path.Value ?? string.Empty;

	if (pathValue.StartsWith("/api/identity", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/identity", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/docs/identity", StringComparison.OrdinalIgnoreCase))
	{
		return "identity";
	}

	if (pathValue.StartsWith("/api/operationlog", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/operationlog", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/docs/operationlog", StringComparison.OrdinalIgnoreCase))
	{
		return "operationlog";
	}

	if (pathValue.StartsWith("/api/test", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/test", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/docs/test", StringComparison.OrdinalIgnoreCase))
	{
		return "test";
	}

	if (pathValue.StartsWith("/api/files", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/docs/fileupload", StringComparison.OrdinalIgnoreCase))
	{
		return "fileupload";
	}

	return "gateway";
}

static GatewayRequestBucket ResolveRequestBucket(PathString path)
{
	var pathValue = path.Value ?? string.Empty;

	if (pathValue.StartsWith("/docs", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
		pathValue.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase))
	{
		return GatewayRequestBucket.Docs;
	}

	if (pathValue.Contains("/login", StringComparison.OrdinalIgnoreCase) ||
		pathValue.Contains("/register", StringComparison.OrdinalIgnoreCase) ||
		pathValue.Contains("/refresh", StringComparison.OrdinalIgnoreCase) ||
		pathValue.Contains("/logout", StringComparison.OrdinalIgnoreCase) ||
		pathValue.Contains("/email", StringComparison.OrdinalIgnoreCase))
	{
		return GatewayRequestBucket.Auth;
	}

	return GatewayRequestBucket.Api;
}

enum GatewayRequestBucket
{
	Api,
	Auth,
	Docs
}

