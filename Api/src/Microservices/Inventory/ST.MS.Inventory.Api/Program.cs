using NLog;
using Scalar.AspNetCore;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.EventBus.OperationLog;
using ST.Infra.EventBus.RabbitMQ.Extensions;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.IntegrationEvents.Payment;
using ST.Infra.Redis.Extensions;
using ST.Infra.ReliableMessaging.Extensions;
using ST.MS.Inventory.Application;
using ST.MS.Inventory.Application.Options;
using ST.MS.Inventory.Application.Services;
using ST.MS.Inventory.Domain;
using ST.MS.Inventory.Infra;
using ST.Shared.Module;
using ST.Shared.WebApi.Extensions.OpenApi;

try
{
	var builder = WebApplication.CreateBuilder(args);

	var modules = new ISharedModule[] { new ApplicationModule(), new DomainModule(), new InfraModule() };

	builder.AddServiceDefaults();
	builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
	{
		metrics.AddMeter("ST.Inventory");
		metrics.AddMeter("ST.Outbox");
	});
	builder.AddSharedWebApi(modules);
	builder.Services.AddRabbitMqOperationLogSink(builder.Configuration);

	// 注册 Outbox Publisher 后台服务
	builder.Services.AddOutboxPublisher(builder.Configuration);

	// 注册 RabbitMQ EventBus（用于消费集成事件）
	builder.Services.AddRabbitMqEventBus(builder.Configuration);

	// 注册 Redis 库存预扣服务
	builder.Services.AddInventoryRedis();

	// 注册库存 Redis 同步服务（启动时全量同步 + 定时同步 + Redis 恢复检测）
	builder.Services.Configure<InventorySyncOptions>(builder.Configuration.GetSection(InventorySyncOptions.SectionName));
	builder.Services.AddHostedService<InventoryRedisSyncService>();

	builder.Services.AddOpenApi(options =>
	{
		options.AddDocumentTransformer<BearerAuthDocumentTransformer>();
	});

	var app = builder.Build();

	app.MapDefaultEndpoints();
	app.UseSharedWebApi(modules);

	// 订阅集成事件
	var eventBus = app.Services.GetRequiredService<IEventBus>();
	eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedHandler>();
	eventBus.Subscribe<OrderCanceledIntegrationEvent, OrderCanceledHandler>();
	eventBus.Subscribe<PaymentSucceededIntegrationEvent, InventoryPaymentSucceededHandler>();

	if (app.Environment.IsDevelopment())
	{
		app.MapOpenApi();

		var scalarToken = builder.Configuration.GetValue<string>("Scalar:Token");
		app.MapScalarApiReference(options =>
		{
			options.DefaultFonts = true;
			options.Layout = ScalarLayout.Classic;
			options.Theme = ScalarTheme.Kepler;

			if (!string.IsNullOrWhiteSpace(scalarToken))
			{
				options.AddHttpAuthentication(BearerAuthDocumentTransformer.SchemeName, scheme => scheme.WithToken(scalarToken));
			}
		});

		app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
	}

	app.Run();
}
catch (Exception ex)
{
	Console.WriteLine(ex);
	LogManager.GetCurrentClassLogger().Error(ex);
}
finally
{
	LogManager.Shutdown();
}
