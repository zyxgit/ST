using System.Text;
using NLog;
using NLog.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using ST.Infra.EntityFramework.CodeFirst;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.Infra.EventBus.OperationLog;
using ST.Infra.EventBus.RabbitMQ.Config;
using ST.Infra.Repository.Interface;
using ST.MS.OperationLog.Consumer;
using ST.MS.OperationLog.Consumer.Infrastructure;
using ST.MS.OperationLog.Infra.DbContext;
using ST.Shared.Configuration;
using ST.Shared.Security;

Console.OutputEncoding = Encoding.UTF8;
var builder = Host.CreateApplicationBuilder(args);

ConfigureSharedConfiguration(builder);

builder.Logging.ClearProviders();
builder.Logging.AddNLog();

// ── OpenTelemetry Metrics ───────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
	.WithMetrics(metrics =>
	{
		metrics.AddMeter("ST.OperationLog.Consumer");
		metrics.AddRuntimeInstrumentation();
	});

var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);
if (useOtlpExporter)
{
	builder.Services.AddOpenTelemetry().UseOtlpExporter();
}

LogManager.Setup().LoadConfigurationFromFile(Path.Combine(AppContext.BaseDirectory, "NLog", "nlog.config"));

builder.Services.AddNpgsqlDbContextFromConfig<OperationLogDbContext>();
builder.Services.AddSingleton<ICurrentUserIdAccessor, BackgroundCurrentUserIdAccessor>();
builder.Services.AddSingleton<ICurrentTenantAccessor, BackgroundCurrentTenantAccessor>();

builder.Services.AddSingleton(sp =>
{
	var opt = new RabbitMqOperationLogOptions();
	builder.Configuration.GetSection("RabbitMQ:OperationLog").Bind(opt);
	RabbitMqConnectionStringBinder.ApplyReference(builder.Configuration, opt);
	return opt;
});

// 注册死信服务
builder.Services.AddSingleton<DeadLetterService>();
builder.Services.AddSingleton<ST.MS.OperationLog.Application.IServices.IDeadLetterService>(sp =>
	sp.GetRequiredService<DeadLetterService>());

// 注册归档服务
builder.Services.Configure<ST.MS.OperationLog.Infra.Archive.OperationLogArchiveOptions>(
	builder.Configuration.GetSection(ST.MS.OperationLog.Infra.Archive.OperationLogArchiveOptions.SectionName));
builder.Services.AddSingleton<ST.MS.OperationLog.Infra.Archive.IArchiveService, ST.MS.OperationLog.Infra.Archive.LocalArchiveService>();
builder.Services.AddHostedService<OperationLogArchiveJob>();

// 根据配置选择消费者模式
var enableBatchConsumer = builder.Configuration.GetValue("RabbitMQ:OperationLog:EnableBatchConsumer", true);
if (enableBatchConsumer)
{
	builder.Services.AddHostedService<BatchOperationLogConsumerHostedService>();
}
else
{
	builder.Services.AddHostedService<RabbitMqOperationLogConsumerHostedService>();
}

var host = builder.Build();
await host.ExecuteCodeFirstExecutorsAsync();
await host.RunAsync();

static void ConfigureSharedConfiguration(HostApplicationBuilder builder)
{
	builder.Configuration.AddStSharedDefaults();
}
