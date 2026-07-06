namespace ST.Infra.ReliableMessaging.Configurations;

/// <summary>
/// ModelBuilder 扩展，供业务服务在自己的 DbContext 中快捷注册 Outbox / Inbox 实体配置。
/// </summary>
public static class ModelBuilderExtensions
{
	/// <summary>
	/// 注册 outbox_messages 与 inbox_messages 表的实体配置。
	/// </summary>
	public static ModelBuilder ApplyReliableMessaging(this ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfiguration(new OutboxMessageEntityTypeConfiguration());
		modelBuilder.ApplyConfiguration(new InboxMessageEntityTypeConfiguration());
		return modelBuilder;
	}
}
