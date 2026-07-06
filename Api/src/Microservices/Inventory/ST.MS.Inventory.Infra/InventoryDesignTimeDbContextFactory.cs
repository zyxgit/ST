using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.Inventory.Infra.DbContext;

namespace ST.MS.Inventory.Infra;

public sealed class InventoryDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<InventoryDbContext>
{
	protected override InventoryDbContext CreateDbContext(DbContextOptions options, string[] args)
	{
		return new InventoryDbContext(options);
	}
}
