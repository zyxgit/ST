using ST.Infra.EntityFramework.DbContextBase;
using ST.Infra.ReliableMessaging.Configurations;

namespace ST.Infra.ReliableMessaging.DbContext;

/// <summary>
/// 可靠消息专用 DbContext。
/// 用于 Outbox Publisher 后台任务扫描待发送消息，以及 Inbox 消息查询。
/// 业务服务若需原子写入 Outbox 消息，应在自己的 DbContext 中添加 DbSet&lt;OutboxMessage&gt;
/// 并调用 modelBuilder.ApplyReliableMessaging() 注册实体配置。
/// </summary>
public sealed class ReliableMessagingDbContext : EfDbContextBase
{
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
	public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

	public ReliableMessagingDbContext(DbContextOptions<ReliableMessagingDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyReliableMessaging();
		base.OnModelCreating(modelBuilder);
	}
}
