using NLog;
using Scalar.AspNetCore;
using ST.MS.FileUpload.Api.Filters;
using ST.MS.FileUpload.Application;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Services;
using ST.MS.FileUpload.Infra;
using ST.MS.FileUpload.Infra.Services;
using ST.Shared.Module;
using ST.Shared.WebApi.Extensions.OpenApi;

try
{
    var builder = WebApplication.CreateBuilder(args);

    var modules = new ISharedModule[]
    {
        new ApplicationModule(),
        new DomainModule(),
        new InfraModule()
    };

    builder.AddServiceDefaults();
    builder.AddSharedWebApi(modules);

    // 文件存储配置
    builder.Services.Configure<FileStorageOptions>(
        builder.Configuration.GetSection(FileStorageOptions.SectionName));

    // 文件存储工厂：根据配置选择存储实现
    builder.Services.AddSingleton<IFileStorageService>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<FileStorageOptions>>();
        return options.Value.Type switch
        {
            FileStorageType.MinIO => ActivatorUtilities.CreateInstance<MinIOFileStorageService>(sp),
            FileStorageType.OSS => ActivatorUtilities.CreateInstance<OSSFileStorageService>(sp),
            _ => ActivatorUtilities.CreateInstance<LocalFileStorageService>(sp)
        };
    });

    // 文件上传验证过滤器
    builder.Services.AddScoped<FileUploadValidationFilter>();

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerAuthDocumentTransformer>();
    });

    var app = builder.Build();

    app.MapDefaultEndpoints();
    app.UseSharedWebApi(modules);

    // 静态文件：允许通过 URL 访问上传目录
    var uploadsPath = Path.GetFullPath("uploads");
    Directory.CreateDirectory(uploadsPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
        RequestPath = "/uploads",
        ServeUnknownFileTypes = true // 支持所有 MIME 类型
    });

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
                options.AddHttpAuthentication(
                    BearerAuthDocumentTransformer.SchemeName,
                    scheme => scheme.WithToken(scalarToken));
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
