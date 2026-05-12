using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.CodeFirst;
using ST.MS.Test.Domain.Entities;
using ST.MS.Test.Infra.DbContext;

namespace ST.MS.Test.Infra.Seeds;

public sealed class TestSampleDataSeed : IDbContextSeed<AppDbContext>
{
	public string Name => "seed-test-samples";

	public int Order => 100;

	public async Task SeedAsync(AppDbContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
	{
		var exists = await dbContext.Tests.AnyAsync(cancellationToken);
		if (exists)
		{
			return;
		}

		dbContext.Tests.AddRange(
			new TestEntity("模板示例数据", "系统启动时自动插入的 EF 种子数据", 1),
			new TestEntity("第二条示例数据", "你可以把这里替换成真实业务初始化数据", 2));
	}
}
