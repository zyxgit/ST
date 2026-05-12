using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ST.Infra.EntityFramework.Npgsql.DesignTime;

public abstract class NpgsqlDesignTimeDbContextFactoryBase<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
	where TDbContext : DbContext
{
	public TDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();

		optionsBuilder.UseNpgsql(GetConnectionString(args));

		// 禁止迁移生成外键（根据项目约定，外键由业务/脚本控制）
		optionsBuilder.ReplaceService<IMigrationsSqlGenerator, NoForeignKeySqlGenerator>();

		Configure(optionsBuilder, args);

		return CreateDbContext(optionsBuilder.Options, args);
	}

	protected virtual void Configure(DbContextOptionsBuilder<TDbContext> optionsBuilder, string[] args)
	{
	}

	protected abstract string GetConnectionString(string[] args);

	protected abstract TDbContext CreateDbContext(DbContextOptions options, string[] args);
}
