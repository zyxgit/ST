using NLog;
using Scalar.AspNetCore;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.EventBus.OperationLog;
using ST.Infra.EventBus.RabbitMQ.Extensions;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.ReliableMessaging.Extensions;
using ST.MS.Payment.Application;
using ST.MS.Payment.Application.Services;
using ST.MS.Payment.Domain;
using ST.MS.Payment.Infra;
using ST.Shared.Module;
using ST.Shared.WebApi.Extensions.OpenApi;

try
{
	var builder = WebApplication.CreateBuilder(args);

	var modules = new ISharedModule[] { new ApplicationModule(), new DomainModule(), new InfraModule() };

	builder.AddServiceDefaults();
	builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
	{
		metrics.AddMeter("ST.Payment");
		metrics.AddMeter("ST.Outbox");
	});
	builder.AddSharedWebApi(modules);
	builder.Services.AddRabbitMqOperationLogSink(builder.Configuration);

	// 注册 Outbox Publisher
	builder.Services.AddOutboxPublisher(builder.Configuration);

	// 注册 RabbitMQ EventBus
	builder.Services.AddRabbitMqEventBus(builder.Configuration);

	builder.Services.AddOpenApi(options =>
	{
		options.AddDocumentTransformer<BearerAuthDocumentTransformer>();
	});

	var app = builder.Build();

	app.MapDefaultEndpoints();
	app.UseSharedWebApi(modules);

	// 订阅 OrderCreated 事件
	var eventBus = app.Services.GetRequiredService<IEventBus>();
	eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedHandler>();

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
