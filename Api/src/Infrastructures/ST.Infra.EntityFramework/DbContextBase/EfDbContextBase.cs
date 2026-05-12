using Microsoft.EntityFrameworkCore;

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

	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSnakeCaseNamingConvention();

	}

}
