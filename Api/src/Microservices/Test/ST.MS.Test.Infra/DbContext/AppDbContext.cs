using ST.Infra.ReliableMessaging.Configurations;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Test.Domain.Entities;

namespace ST.MS.Test.Infra.DbContext;

public class AppDbContext : EfDbContextBase
{
	public DbSet<TestEntity> Tests => Set<TestEntity>();

	// 可靠消息表（Outbox / Inbox）
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
	public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

	public AppDbContext(DbContextOptions options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// 先配置可靠消息模型，避免被默认字符串长度约束截断
		modelBuilder.ApplyReliableMessaging();
		base.OnModelCreating(modelBuilder);
	}
}
