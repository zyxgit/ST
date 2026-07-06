using Microsoft.Extensions.DependencyInjection;
using ST.Shared.Module;

namespace ST.MS.Order.Application;

public class ApplicationModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);
	}
}
