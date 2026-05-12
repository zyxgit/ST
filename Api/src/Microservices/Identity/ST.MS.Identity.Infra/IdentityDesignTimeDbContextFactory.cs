using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Const;

namespace ST.MS.Identity.Infra;

public sealed class IdentityDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<IdentityDbContext>
{
	protected override string GetConnectionString(string[] args)
	{
		return Environment.GetEnvironmentVariable(SettingPrefixContants.Database_ConnectionString_Env)
			?? throw new InvalidOperationException($"Design-time database connection string is not configured. Set the '{SettingPrefixContants.Database_ConnectionString_Env}' environment variable.");
	}

	protected override IdentityDbContext CreateDbContext(DbContextOptions options, string[] args)
	{
		return new IdentityDbContext(options);
	}
}
