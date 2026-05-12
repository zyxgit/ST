using ST.MS.Test.Domain.Entities;

namespace ST.MS.Test.Infra.DbContext;

public class AppDbContext : EfDbContextBase
{
	public DbSet<TestEntity> Tests => Set<TestEntity>();

	public AppDbContext(DbContextOptions options) : base(options)
	{
	}
}
