using Microsoft.Extensions.DependencyInjection;
using ST.Shared.Module;

namespace ST.MS.FileUpload.Application;

public sealed class ApplicationModule : ServiceModule
{
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
    }
}
