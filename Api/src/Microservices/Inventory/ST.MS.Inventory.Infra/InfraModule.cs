using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.MS.Inventory.Infra.DbContext;
using ST.MS.Inventory.Infra.Seeds;
using ST.Shared.Module;

namespace ST.MS.Inventory.Infra;

public sealed class InfraModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);

		services.AddNpgsqlDbContextFromConfig<InventoryDbContext>(seeds =>
		{
			seeds.Add<InventorySampleDataSeed>();
		});
	}
}
