using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ST.MS.Test.Infra.DbContext;
using ST.Shared.Const;

namespace ST.MS.Test.Infra;

public sealed class TestDesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
	public AppDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

		// 优先使用环境变量（适合 CI 或本机覆盖）：
		// - Database__ConnectionString
		// 回退值与本仓库的本地 Aspire 默认一致（见 appsettings.Development.json）。
		var connectionString =
			Environment.GetEnvironmentVariable(SettingPrefixContants.Database_ConnectionString_Env)
			?? "Host=localhost;Port=15432;Username=postgres;Password=pw123456;Database=st_test;";

		optionsBuilder.UseNpgsql(connectionString);

		return new AppDbContext(optionsBuilder.Options);
	}
}
