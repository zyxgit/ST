using Microsoft.Extensions.DependencyInjection;
using ST.MS.FileUpload.Application.IServices;
using ST.MS.FileUpload.Application.Services;
using ST.Shared.Module;

namespace ST.MS.FileUpload.Application;

public sealed class ApplicationModule : ServiceModule
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // 注册分片上传服务
        services.AddScoped<IMultipartUploadService, MultipartUploadService>();

        base.ConfigureServices(services);
    }
}
