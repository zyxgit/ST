using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.MS.Payment.Infra.DbContext;
using ST.Shared.Module;

namespace ST.MS.Payment.Infra;

public sealed class InfraModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);

		services.AddNpgsqlDbContextFromConfig<PaymentDbContext>();
	}
}
