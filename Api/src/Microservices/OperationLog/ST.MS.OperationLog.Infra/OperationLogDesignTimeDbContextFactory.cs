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
			?? "Host=localhost;Port=15432;Username=postgres;Password=pw123456;Database=st_operationlog;";
	}

	protected override OperationLogDbContext CreateDbContext(DbContextOptions options, string[] args)
	{
		return new OperationLogDbContext(options);
	}
}

