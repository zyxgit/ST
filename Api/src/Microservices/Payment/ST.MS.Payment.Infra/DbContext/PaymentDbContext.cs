using ST.Infra.ReliableMessaging.Configurations;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Payment.Domain.Entities;

namespace ST.MS.Payment.Infra.DbContext;

/// <summary>
/// Payment 服务 DbContext。
/// </summary>
public class PaymentDbContext : EfDbContextBase
{
	public DbSet<Domain.Entities.Payment> Payments => Set<Domain.Entities.Payment>();

	// 可靠消息表（Outbox / Inbox）
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
	public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

	public PaymentDbContext(DbContextOptions options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyReliableMessaging();

		modelBuilder.Entity<Domain.Entities.Payment>(entity =>
		{
			entity.ToTable("payments");
			entity.Property(p => p.Amount).HasPrecision(18, 2);
			entity.Property(p => p.FailureReason).HasMaxLength(500);
			entity.HasIndex(p => p.OrderId);
			entity.HasIndex(p => p.Status);
		});

		base.OnModelCreating(modelBuilder);
	}
}
