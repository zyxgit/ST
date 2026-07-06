namespace ST.Infra.ReliableMessaging.Configurations;

/// <summary>
/// InboxMessage 实体配置。
/// </summary>
public sealed class InboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<InboxMessage>
{
	public void Configure(EntityTypeBuilder<InboxMessage> entity)
	{
		entity.ToTable("inbox_messages");
		entity.HasKey(x => x.Id);

		// MessageId + Consumer 构成唯一约束，保证幂等消费
		entity.HasIndex(x => new { x.MessageId, x.Consumer })
			.IsUnique()
			.HasDatabaseName("ix_inbox_messages_message_id_consumer");

		entity.HasIndex(x => x.ProcessedAtUtc);

		entity.Property(x => x.Consumer).HasMaxLength(300);
		entity.Property(x => x.EventType).HasMaxLength(500);
		entity.Property(x => x.ErrorMessage).HasColumnType("text");
	}
}
