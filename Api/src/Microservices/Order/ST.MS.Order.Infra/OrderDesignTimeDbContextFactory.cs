using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.Order.Infra.DbContext;

namespace ST.MS.Order.Infra;

public sealed class OrderDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<OrderDbContext>
{
	protected override OrderDbContext CreateDbContext(DbContextOptions options, string[] args)
	{
		return new OrderDbContext(options);
	}
}
