using System.Text;
using NLog;
using NLog.Extensions.Logging;
using ST.Infra.EntityFramework.CodeFirst;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.Infra.EventBus.OperationLog;
using ST.Infra.Repository.Interface;
using ST.MS.OperationLog.Consumer;
using ST.MS.OperationLog.Consumer.Infrastructure;
using ST.MS.OperationLog.Infra.DbContext;
using ST.Shared.Configuration;

Console.OutputEncoding = Encoding.UTF8;
var builder = Host.CreateApplicationBuilder(args);

ConfigureSharedConfiguration(builder);

builder.Logging.ClearProviders();
builder.Logging.AddNLog();

LogManager.Setup().LoadConfigurationFromFile(Path.Combine(AppContext.BaseDirectory, "NLog", "nlog.config"));

builder.Services.AddNpgsqlDbContextFromConfig<OperationLogDbContext>();
builder.Services.AddSingleton<ICurrentUserIdAccessor, BackgroundCurrentUserIdAccessor>();

builder.Services.AddSingleton(sp =>
{
	var opt = new RabbitMqOperationLogOptions();
	builder.Configuration.GetSection("RabbitMQ:OperationLog").Bind(opt);
	return opt;
});

builder.Services.AddHostedService<RabbitMqOperationLogConsumerHostedService>();

var host = builder.Build();
await host.ExecuteCodeFirstExecutorsAsync();
await host.RunAsync();

static void ConfigureSharedConfiguration(HostApplicationBuilder builder)
{
	builder.Configuration.AddStSharedDefaults();
}
