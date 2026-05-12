using NLog;
using Scalar.AspNetCore;
using ST.Infra.EventBus.OperationLog;
using ST.MS.Test.Application;
using ST.MS.Test.Domain;
using ST.MS.Test.Infra;
using ST.Shared.Module;
using ST.Shared.WebApi.Extensions.OpenApi;

try
{
	var builder = WebApplication.CreateBuilder(args);

	// modules 会决定 Autofac 扫描/注册哪些程序集，也可承载分层的额外注入逻辑（如 DbContext）。
	var modules = new ISharedModule[] { new ApplicationModule(), new DomainModule(), new InfraModule() };

	builder.AddServiceDefaults();
	builder.AddSharedWebApi(modules);
	builder.Services.AddRabbitMqOperationLogSink(builder.Configuration);

	// 当调用发生在 host 项目本身时，生成器能看到并替换为带 XML 支持的版本
	// 封装到类库后，调用站点变成了类库里的方法，拦截器匹配不上。
	builder.Services.AddOpenApi(options =>
	{
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
