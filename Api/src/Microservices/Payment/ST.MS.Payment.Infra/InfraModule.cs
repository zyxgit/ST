using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.MS.Payment.Infra.DbContext;
using ST.Shared.Module;

namespace ST.MS.Payment.Infra;

public sealed class InfraModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);

		services.AddNpgsqlDbContextFromConfig<PaymentDbContext>();

		// 注册基于 PaymentDbContext 的可靠消息 Store（与业务数据同一事务）
		services.AddScoped<IOutboxStore, DbContextOutboxStore<PaymentDbContext>>();
		services.AddScoped<IInboxStore, DbContextInboxStore<PaymentDbContext>>();
	}
}
