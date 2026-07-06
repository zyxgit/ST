using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.MS.OperationLog.Infra.Archive;
using ST.MS.OperationLog.Infra.DbContext;
using ST.Shared.Module;

namespace ST.MS.OperationLog.Infra;

public sealed class InfraModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);

		services.AddNpgsqlDbContextFromConfig<OperationLogDbContext>();

		// 注册归档配置
		services.AddOptions<OperationLogArchiveOptions>()
			.BindConfiguration(OperationLogArchiveOptions.SectionName);

		// 注册归档服务
		services.AddSingleton<IArchiveService, LocalArchiveService>();
	}
}
