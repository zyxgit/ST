using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.MS.FileUpload.Infra.DbContext;
using ST.Shared.Module;

namespace ST.MS.FileUpload.Infra;

public sealed class InfraModule : ServiceModule
{
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        services.AddNpgsqlDbContextFromConfig<FileUploadDbContext>();
    }
}
