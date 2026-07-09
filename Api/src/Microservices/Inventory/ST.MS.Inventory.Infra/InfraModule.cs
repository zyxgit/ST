using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.Infra.ReliableMessaging.Abstractions;
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

		// 注册基于 InventoryDbContext 的可靠消息 Store（与业务数据同一事务）
		services.AddScoped<IOutboxStore, DbContextOutboxStore<InventoryDbContext>>();
		services.AddScoped<IInboxStore, DbContextInboxStore<InventoryDbContext>>();
	}
}
