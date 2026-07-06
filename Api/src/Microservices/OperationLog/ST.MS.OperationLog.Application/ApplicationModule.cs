using Microsoft.Extensions.DependencyInjection;
using ST.MS.OperationLog.Application.IServices;
using ST.MS.OperationLog.Application.Services;
using ST.Shared.Module;

namespace ST.MS.OperationLog.Application;

public sealed class ApplicationModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		// 注册死信查询服务
		services.AddScoped<IDeadLetterQueryService, DeadLetterQueryService>();

		base.ConfigureServices(services);
	}
}
