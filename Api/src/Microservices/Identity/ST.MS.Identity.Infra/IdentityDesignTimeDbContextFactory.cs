using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.Identity.Infra.DbContext;

namespace ST.MS.Identity.Infra;

public sealed class IdentityDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<IdentityDbContext>
{
    protected override IdentityDbContext CreateDbContext(DbContextOptions options, string[] args)
    {
        return new IdentityDbContext(options);
    }
}
