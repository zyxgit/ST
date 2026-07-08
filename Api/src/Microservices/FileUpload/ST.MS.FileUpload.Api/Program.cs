using NLog;
using Scalar.AspNetCore;
using ST.MS.FileUpload.Api.Filters;
using ST.MS.FileUpload.Application;
using ST.MS.FileUpload.Application.Services;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Services;
using ST.MS.FileUpload.Infra;
using ST.MS.FileUpload.Infra.Services;
using ST.Infra.Redis.Extensions;
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
    builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddMeter("ST.FileUpload"));
    builder.AddSharedWebApi(modules);

    // Redis（用于分片上传状态记录）
    builder.Services.AddRedisInfra(builder.Configuration);

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

    // 签名 URL 服务
    builder.Services.AddSingleton<ISignedUrlService, SignedUrlService>();

    // 分片合并后台服务
    builder.Services.Configure<MultipartMergeOptions>(
        builder.Configuration.GetSection(MultipartMergeOptions.SectionName));
    builder.Services.AddHostedService<MultipartMergeService>();

    // 分片清理后台服务
    builder.Services.Configure<MultipartCleanupOptions>(
        builder.Configuration.GetSection(MultipartCleanupOptions.SectionName));
    builder.Services.AddHostedService<MultipartCleanupService>();

    // 文件上传验证过滤器
    builder.Services.AddScoped<FileUploadValidationFilter>();

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerAuthDocumentTransformer>();
    });

    var app = builder.Build();

    app.MapDefaultEndpoints();
    app.UseSharedWebApi(modules);

    // 确保上传目录存在（静态文件中间件已移除，文件统一通过 /api/files/{id}/download 访问）
    var uploadsPath = Path.GetFullPath("uploads");
    Directory.CreateDirectory(uploadsPath);

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
