using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.OperationLog.Infra.DbContext;
using ST.Shared.Const;

namespace ST.MS.OperationLog.Infra;

public sealed class OperationLogDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<OperationLogDbContext>
{
	protected override string GetConnectionString(string[] args)
	{
		return Environment.GetEnvironmentVariable(SettingPrefixContants.Database_ConnectionString_Env)
			?? throw new InvalidOperationException($"Design-time database connection string is not configured. Set the '{SettingPrefixContants.Database_ConnectionString_Env}' environment variable.");
	}

	protected override OperationLogDbContext CreateDbContext(DbContextOptions options, string[] args)
	{
		return new OperationLogDbContext(options);
	}
}
