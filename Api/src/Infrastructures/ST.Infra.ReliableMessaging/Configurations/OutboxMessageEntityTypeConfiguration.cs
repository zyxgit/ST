namespace ST.Infra.ReliableMessaging.Configurations;

/// <summary>
/// OutboxMessage 实体配置。
/// </summary>
public sealed class OutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
	public void Configure(EntityTypeBuilder<OutboxMessage> entity)
	{
		entity.ToTable("outbox_messages");
		entity.HasKey(x => x.Id);

		entity.HasIndex(x => x.Status);
		entity.HasIndex(x => x.NextRetryAtUtc);
		entity.HasIndex(x => new { x.Status, x.NextRetryAtUtc })
			.HasDatabaseName("ix_outbox_messages_status_next_retry");

		entity.Property(x => x.EventType).HasMaxLength(500);
		entity.Property(x => x.Payload).HasColumnType("jsonb");
		entity.Property(x => x.ErrorMessage).HasColumnType("text");
		entity.Property(x => x.TraceId).HasMaxLength(64);
	}
}
