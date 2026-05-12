using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.MS.Test.Infra.DbContext;
using ST.MS.Test.Infra.Seeds;
using ST.Shared.Module;

namespace ST.MS.Test.Infra;

public sealed class InfraModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);

		services.AddNpgsqlDbContextFromConfig<AppDbContext>(seeds =>
		{
			seeds.Add<TestSampleDataSeed>();
		});
	}
}
