using Microsoft.Extensions.DependencyInjection;
using ST.Shared.Module;

namespace ST.MS.Identity.Application;

public class ApplicationModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);
	}
}
