using NLog;
using Scalar.AspNetCore;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.EventBus.OperationLog;
using ST.Infra.EventBus.RabbitMQ.Extensions;
using ST.Infra.IntegrationEvents.Inventory;
using ST.Infra.IntegrationEvents.Payment;
using ST.Infra.ReliableMessaging.Extensions;
using ST.MS.Order.Application;
using ST.MS.Order.Application.Services;
using ST.MS.Order.Domain;
using ST.MS.Order.Infra;
using ST.Shared.Module;
using ST.Shared.WebApi.Extensions.OpenApi;

try
{
	var builder = WebApplication.CreateBuilder(args);

	var modules = new ISharedModule[] { new ApplicationModule(), new DomainModule(), new InfraModule() };

	builder.AddServiceDefaults();
	builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
	{
		metrics.AddMeter("ST.Order");
		metrics.AddMeter("ST.Outbox");
	});
	builder.AddSharedWebApi(modules);
	builder.Services.AddRabbitMqOperationLogSink(builder.Configuration);

	// 注册 Outbox Publisher 后台服务
	builder.Services.AddOutboxPublisher(builder.Configuration);

	// 注册订单超时自动取消后台服务
	builder.Services.Configure<OrderTimeoutOptions>(builder.Configuration.GetSection(OrderTimeoutOptions.SectionName));
	builder.Services.AddHostedService<OrderTimeoutCheckService>();

	// 注册 RabbitMQ EventBus（用于消费 Inventory 集成事件）
	builder.Services.AddRabbitMqEventBus(builder.Configuration);

	builder.Services.AddOpenApi(options =>
	{
		options.AddDocumentTransformer<BearerAuthDocumentTransformer>();
	});

	var app = builder.Build();

	app.MapDefaultEndpoints();
	app.UseSharedWebApi(modules);

	// 订阅 Inventory 集成事件
	var eventBus = app.Services.GetRequiredService<IEventBus>();
	eventBus.Subscribe<InventoryFrozenIntegrationEvent, InventoryFrozenHandler>();
	eventBus.Subscribe<InventoryFreezeFailedIntegrationEvent, InventoryFreezeFailedHandler>();
	eventBus.Subscribe<PaymentSucceededIntegrationEvent, PaymentSucceededHandler>();
	eventBus.Subscribe<PaymentFailedIntegrationEvent, PaymentFailedHandler>();

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
