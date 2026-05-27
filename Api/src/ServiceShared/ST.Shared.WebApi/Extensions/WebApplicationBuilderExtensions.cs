using System.Threading.RateLimiting;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Migrations;
using NLog;
using NLog.Web;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ST.Infra.EntityFramework.CodeFirst;
using ST.Infra.EntityFramework.Npgsql;
using ST.Infra.Redis.Extensions;
using ST.Infra.Tasks.Extensions;
using ST.Shared.Configuration;
using ST.Shared.Module;
using ST.Shared.OperationLog;
using ST.Shared.WebApi.Authentication;
using ST.Shared.WebApi.Autofac;
using ST.Shared.WebApi.Middleware;
using ST.Shared.WebApi.OperationLog;

namespace ST.Shared.WebApi.Extensions;

public static class WebApplicationBuilderExtensions
{
	/// <summary>
	/// 添加共享的 WebApi
	/// </summary>
	/// <param name="builder"></param>
	/// <param name="modules">解决未调用服务未加载进CLR问题</param>
	/// <returns></returns>
	public static WebApplicationBuilder AddSharedWebApi(this WebApplicationBuilder builder, params ISharedModule[] modules)
	{
		ConfigureSharedConfiguration(builder);

		// 1. 清空默认日志
		builder.Logging.ClearProviders();

		// 2. 使用 NLog
		builder.Host.UseNLog(new NLogAspNetCoreOptions
		{
			CaptureMessageProperties = true,
			CaptureMessageTemplates = true,
			IncludeScopes = true
		});

		LogManager.Setup().LoadConfigurationFromFile(Path.Combine(AppContext.BaseDirectory, "NLog", "nlog.config"));

		var config = NLog.LogManager.Configuration;

		// OpenTelemetry LoggerProvider 被 ClearProviders 清除后重新注册（仅 logging），
		// 让 ILogger 日志同时走 NLog + OTLP。Metrics/Tracing/UseOtlpExporter 由 ServiceDefaults 处理。
		if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
		{
			builder.Logging.AddOpenTelemetry(logging =>
			{
				logging.IncludeFormattedMessage = true;
				logging.IncludeScopes = true;
			});
		}

		// OperationLog：默认注册 No-Op Sink + Dispatcher（避免未启用落库实现时启动失败）
		builder.Services.TryAddSingleton<IOptions<OperationLogOptions>>(_ =>
		{
			var opt = new OperationLogOptions();
			builder.Configuration.GetSection("OperationLog").Bind(opt);
			return Options.Create(opt);
		});
		builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOperationLogSink, NullOperationLogSink>());
		builder.Services.TryAddSingleton<IOperationLogDispatcher, OperationLogDispatcher>();

		builder.Services.AddSharedUserContext();
		builder.Services.AddSharedJwtAuthentication(builder.Configuration);

		// Request logging 配置（默认不记录 Body，避免敏感信息泄露）
		builder.Services.Configure<RequestLoggingOptions>(builder.Configuration.GetSection("RequestLogging"));

		// CORS
		builder.Services.AddCors(options =>
		{
			options.AddPolicy("st-default", policy =>
			{
				var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
				var useAllowAllInDev = builder.Environment.IsDevelopment();

				if (allowedOrigins.Length > 0)
				{
					policy.WithOrigins(allowedOrigins)
						.AllowAnyHeader()
						.AllowAnyMethod()
						.AllowCredentials();
				}
				else if (useAllowAllInDev)
				{
					policy.AllowAnyOrigin()
						.AllowAnyHeader()
						.AllowAnyMethod();
				}
				else
				{
					// 生产环境默认不放行任何来源（需要在 Cors:AllowedOrigins 配置允许的域名）
					policy.SetIsOriginAllowed(_ => false);
				}
			});
		});

		// Rate Limiting（默认开启）
		var rateLimiterEnabled = builder.Configuration.GetValue("RateLimiting:Enabled", false);
		if (rateLimiterEnabled)
		{
			var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 60L);
			var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);

			builder.Services.AddRateLimiter(options =>
			{
				options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
				{
					var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
					return RateLimitPartition.GetFixedWindowLimiter(
						ip,
						_ => new FixedWindowRateLimiterOptions
						{
							PermitLimit = (int)Math.Clamp(permitLimit, 1, int.MaxValue),
							Window = TimeSpan.FromSeconds(windowSeconds),
							QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
							QueueLimit = 0
						});
				});
			});
		}

		// Add services to the container.
		builder.Services.AddControllers(options =>
		{
			options.Filters.Add<OperationLogActionFilter>();
		});
		// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
		//将 Autofac 设置为默认的 DI 容器
		builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
			.ConfigureContainer<ContainerBuilder>((context, container) =>
			{
				foreach (var module in modules)
				{
					container.RegisterDependencies(module.Assembly);
				}
			});

		builder.Services.AddRedisInfra(builder.Configuration);

		builder.Services.AddInfraTasks();

		foreach (var module in modules)
		{
			module.ConfigureServices(builder.Services);
		}

		// 在应用级别注册 NoForeignKeySqlGenerator，确保 MigrateAsync() 运行时使用它
		// （ReplaceService 仅影响 DbContextOptions，Migrator 从应用容器解析 IMigrationsSqlGenerator）
		builder.Services.AddSingleton<IMigrationsSqlGenerator, NoForeignKeySqlGenerator>();

		return builder;
	}

	private static void ConfigureSharedConfiguration(WebApplicationBuilder builder)
	{
		// 共享配置作为默认值，优先级低于服务自己的 appsettings / UserSecrets / 环境变量。
		builder.Configuration.AddStSharedDefaults();
	}

	private sealed class NullOperationLogSink : IOperationLogSink
	{
		public string Name => "null";

		public ValueTask EnqueueAsync(OperationLogEntry entry, CancellationToken cancellationToken = default)
			=> ValueTask.CompletedTask;
	}

	public static WebApplication UseSharedWebApi(this WebApplication app, params ISharedModule[] modules)
	{
		// Security headers（在 GlobalException 之前设置，确保异常响应也带上头信息）
		app.Use(async (context, next) =>
		{
			context.Response.Headers["X-Content-Type-Options"] = "nosniff";
			context.Response.Headers["X-Frame-Options"] = "DENY";
			context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
			context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

			await next();
		});

		app.UseMiddleware<GlobalExceptionMiddleware>();
		app.UseMiddleware<RequestLoggingMiddleware>();
		// Configure the HTTP request pipeline.

		app.UseHttpsRedirection();

		if (!app.Environment.IsDevelopment())
		{
			app.UseHsts();
		}

		app.UseCors("st-default");

		var rateLimiterEnabled = app.Configuration.GetValue("RateLimiting:Enabled", false);
		if (rateLimiterEnabled)
		{
			app.UseRateLimiter();
		}

		app.UseAuthentication();
		app.UseAuthorization();

		app.MapControllers();

		foreach (var module in modules)
		{
			module.Configure(app);
		}

		app.Services.ExecuteCodeFirstExecutorsAsync().GetAwaiter().GetResult();

		return app;
	}
}
