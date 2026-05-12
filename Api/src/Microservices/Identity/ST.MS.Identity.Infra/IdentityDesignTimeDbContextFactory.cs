using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Const;

namespace ST.MS.Identity.Infra;

public sealed class IdentityDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<IdentityDbContext>
{
	protected override string GetConnectionString(string[] args)
	{
		// 优先使用环境变量（适合 CI 或本机覆盖）：
		// - Database__ConnectionString
		// 回退值与本仓库的本地 Aspire 默认一致（见 appsettings.Development.json）。
		return Environment.GetEnvironmentVariable(SettingPrefixContants.Database_ConnectionString_Env)
			?? "Host=localhost;Port=15432;Username=postgres;Password=pw123456;Database=st_identity;";
	}

	protected override IdentityDbContext CreateDbContext(DbContextOptions options, string[] args)
	{
		return new IdentityDbContext(options);
	}
}
