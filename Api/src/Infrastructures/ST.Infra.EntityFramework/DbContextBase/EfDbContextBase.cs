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
		//移除外键关系，配合 NoForeignKeySqlGenerator 双保险禁止外键
		modelBuilder.ApplyNoForeignKeys();
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSnakeCaseNamingConvention();
		// ApplyNoForeignKeys() 从模型移除外键，导致模型与迁移快照不一致，
		// 需抑制此警告以允许 MigrateAsync() 正常执行（实际 FK 操作由 NoForeignKeySqlGenerator 处理）。
		optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
	}

}
