using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.CodeFirst;
using ST.MS.Inventory.Domain.Entities;
using ST.MS.Inventory.Infra.DbContext;

namespace ST.MS.Inventory.Infra.Seeds;

public sealed class InventorySampleDataSeed : IDbContextSeed<InventoryDbContext>
{
	/// <summary>
	/// 默认租户 ID，与 Identity 种子数据中的 DefaultTenantId 一致
	/// </summary>
	private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111000");

	public string Name => "seed-inventory-samples";

	public int Order => 100;

	public async Task SeedAsync(InventoryDbContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
	{
		var exists = await dbContext.Skus.AnyAsync(cancellationToken);
		if (exists)
		{
			return;
		}

		var skuA = new Sku(Guid.Parse("00000000-0000-0000-0000-000000000001"), "测试商品A", 100) { TenantId = DefaultTenantId };
		var skuB = new Sku(Guid.Parse("00000000-0000-0000-0000-000000000002"), "测试商品B", 50) { TenantId = DefaultTenantId };
		var skuC = new Sku(Guid.Parse("00000000-0000-0000-0000-000000000003"), "测试商品C", 200) { TenantId = DefaultTenantId };

		dbContext.Skus.AddRange(skuA, skuB, skuC);
	}
}
