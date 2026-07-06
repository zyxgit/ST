using ST.Infra.ReliableMessaging.Configurations;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Inventory.Domain.Entities;

namespace ST.MS.Inventory.Infra.DbContext;

/// <summary>
/// Inventory 服务 DbContext。
/// 包含 Outbox/Inbox 表，支持业务数据与可靠消息在同一事务中提交。
/// </summary>
public class InventoryDbContext : EfDbContextBase
{
	public DbSet<Sku> Skus => Set<Sku>();
	public DbSet<InventoryFreezeRecord> FreezeRecords => Set<InventoryFreezeRecord>();

	// 可靠消息表（Outbox / Inbox）
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
	public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

	public InventoryDbContext(DbContextOptions options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// 先配置可靠消息模型
		modelBuilder.ApplyReliableMessaging();

		// Sku 实体配置
		modelBuilder.Entity<Sku>(entity =>
		{
			entity.ToTable("skus");
			entity.Property(s => s.ProductName).HasMaxLength(200);
			entity.HasIndex(s => s.SkuId).IsUnique();
		});

		// InventoryFreezeRecord 实体配置
		modelBuilder.Entity<InventoryFreezeRecord>(entity =>
		{
			entity.ToTable("inventory_freeze_records");
			entity.HasIndex(r => r.OrderId);
			entity.HasIndex(r => r.SkuId);
			entity.HasIndex(r => new { r.OrderId, r.SkuId }).IsUnique();
		});

		base.OnModelCreating(modelBuilder);
	}
}
