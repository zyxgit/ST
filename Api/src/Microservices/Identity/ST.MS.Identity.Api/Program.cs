using NLog;
using Scalar.AspNetCore;
using ST.Infra.Email.Extensions;
using ST.Infra.EventBus.OperationLog;
using ST.Infra.EventBus.RabbitMQ.Extensions;
using ST.MS.Identity.Application;
using ST.MS.Identity.Application.Options;
using ST.MS.Identity.Domain;
using ST.MS.Identity.Infra;
using ST.Shared.WebApi.Extensions;
using ST.Shared.WebApi.Extensions.OpenApi;

try
{
	var builder = WebApplication.CreateBuilder(args);

	// modules 会决定 Autofac 扫描/注册哪些程序集，也可承载分层的额外注入逻辑（如 DbContext）。
	var modules = new ST.Shared.Module.ISharedModule[]
	{
		new ApplicationModule(),
		new DomainModule(),
		new InfraModule()
	};

	builder.AddServiceDefaults();

	builder.AddSharedWebApi(modules);
	builder.Services.Configure<IdentitySessionOptions>(
		builder.Configuration.GetSection(IdentitySessionOptions.SectionName));
	builder.Services.AddInfraEmail(builder.Configuration);
	builder.Services.AddRabbitMqEventBus(builder.Configuration);
	builder.Services.AddRabbitMqOperationLogSink(builder.Configuration);

	builder.Services.AddOutputCache(options =>
	{
		options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(10)));
	});

	builder.Services.AddOpenApi(options =>
	{
		options.AddSchemaTransformer<EnumXmlCommentSchemaTransformer>();
		options.AddDocumentTransformer<BearerAuthDocumentTransformer>();
	});

	var app = builder.Build();

	app.MapDefaultEndpoints();
	app.UseSharedWebApi(modules);

	if (app.Environment.IsDevelopment())
	{
		app.MapOpenApi();

		var scalarToken = builder.Configuration.GetValue<string>("Scalar:Token");
		app.MapScalarApiReference(options =>
		{
			options.DefaultFonts = true;
			options.Layout = ScalarLayout.Classic;
			options.Theme = ScalarTheme.Kepler;

			// 预填 Bearer Token（仅建议开发环境使用）
			if (!string.IsNullOrWhiteSpace(scalarToken))
			{
				options.AddHttpAuthentication(BearerAuthDocumentTransformer.SchemeName, scheme => scheme.WithToken(scalarToken));
			}
		});

		app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
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
