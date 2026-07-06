using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ST.Infra.EntityFramework.DbContextBase;

public abstract class EfDbContextBase : DbContext
{
	protected EfDbContextBase(DbContextOptions options) : base(options)
	{

	}


	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
		//默认未设置字符串长度
		modelBuilder.ApplyDefaultStringLength();
		//软删除过滤器
		modelBuilder.ApplySoftDeleteQueryFilter();
		//租户数据隔离过滤器（必须在软删除之后，合并两者）
		modelBuilder.ApplyTenantQueryFilter();
		// 注意：不在运行时移除外键关系（ApplyNoForeignKeys），否则 Include/ThenInclude 导航属性
		// 会丢失 JOIN 条件元数据，导致查询退化为笛卡尔积或全表扫描。
		// 外键禁止已由 NoForeignKeySqlGenerator 在迁移 SQL 层面实现，运行时保留模型关系即可。
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSnakeCaseNamingConvention();
		// 抑制模型待变更警告（CodeFirst 迁移场景下模型与快照可能不一致）。
		optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
	}

}
