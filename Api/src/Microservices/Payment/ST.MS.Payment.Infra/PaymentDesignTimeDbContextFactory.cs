using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.Payment.Infra.DbContext;

namespace ST.MS.Payment.Infra;

public sealed class PaymentDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<PaymentDbContext>
{
	protected override PaymentDbContext CreateDbContext(DbContextOptions options, string[] args)
	{
		return new PaymentDbContext(options);
	}
}
