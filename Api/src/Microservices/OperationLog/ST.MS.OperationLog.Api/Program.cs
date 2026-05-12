using NLog;
using Scalar.AspNetCore;
using ST.MS.OperationLog.Application;
using ST.MS.OperationLog.Infra;
using ST.Shared.WebApi.Extensions;
using ST.Shared.WebApi.Extensions.OpenApi;

try
{
	var builder = WebApplication.CreateBuilder(args);

	var modules = new ST.Shared.Module.ISharedModule[]
	{
		new ApplicationModule(),
		new InfraModule()
	};

	builder.AddServiceDefaults();
	builder.AddSharedWebApi(modules);

	builder.Services.AddOutputCache(options =>
	{
		options.AddBasePolicy(policy => policy.Expire(TimeSpan.FromMinutes(5)));
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
