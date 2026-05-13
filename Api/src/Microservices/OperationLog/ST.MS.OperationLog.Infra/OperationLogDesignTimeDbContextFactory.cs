using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.OperationLog.Infra.DbContext;

namespace ST.MS.OperationLog.Infra;

public sealed class OperationLogDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<OperationLogDbContext>
{
    protected override OperationLogDbContext CreateDbContext(DbContextOptions options, string[] args)
    {
        return new OperationLogDbContext(options);
    }
}
