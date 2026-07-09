using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.MS.Order.Infra.DbContext;
using ST.Shared.Module;

namespace ST.MS.Order.Infra;

public sealed class InfraModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);

		services.AddNpgsqlDbContextFromConfig<OrderDbContext>();

		// 注册基于 OrderDbContext 的可靠消息 Store（与业务数据同一事务）
		services.AddScoped<IOutboxStore, DbContextOutboxStore<OrderDbContext>>();
		services.AddScoped<IInboxStore, DbContextInboxStore<OrderDbContext>>();
	}
}
