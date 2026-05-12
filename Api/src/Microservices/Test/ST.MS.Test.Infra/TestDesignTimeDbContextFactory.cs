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
		var connectionString = Environment.GetEnvironmentVariable(SettingPrefixContants.Database_ConnectionString_Env)
			?? throw new InvalidOperationException($"Design-time database connection string is not configured. Set the '{SettingPrefixContants.Database_ConnectionString_Env}' environment variable.");

		optionsBuilder.UseNpgsql(connectionString);

		return new AppDbContext(optionsBuilder.Options);
	}
}
