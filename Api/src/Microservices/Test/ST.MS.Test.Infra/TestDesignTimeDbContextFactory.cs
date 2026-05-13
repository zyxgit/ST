using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.Test.Infra.DbContext;

namespace ST.MS.Test.Infra;

public sealed class TestDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<AppDbContext>
{
    protected override AppDbContext CreateDbContext(DbContextOptions options, string[] args)
    {
        return new AppDbContext(options);
    }
}
