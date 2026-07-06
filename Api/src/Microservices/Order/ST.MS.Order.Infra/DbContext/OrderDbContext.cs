using ST.Infra.ReliableMessaging.Configurations;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Order.Domain.Entities;

namespace ST.MS.Order.Infra.DbContext;

/// <summary>
/// Order 服务 DbContext。
/// 包含 Outbox/Inbox 表，支持业务数据与可靠消息在同一事务中提交。
/// </summary>
public class OrderDbContext : EfDbContextBase
{
	public DbSet<Domain.Entities.Order> Orders => Set<Domain.Entities.Order>();
	public DbSet<OrderItem> OrderItems => Set<OrderItem>();
	public DbSet<SagaInstance> SagaInstances => Set<SagaInstance>();
	public DbSet<SagaStep> SagaSteps => Set<SagaStep>();

	// 可靠消息表（Outbox / Inbox）
	public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
	public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

	public OrderDbContext(DbContextOptions options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// 先配置可靠消息模型，避免被默认字符串长度约束截断
		modelBuilder.ApplyReliableMessaging();

		// Order 实体配置
		modelBuilder.Entity<Domain.Entities.Order>(entity =>
		{
			entity.ToTable("orders");
			entity.Property(o => o.OrderNo).HasMaxLength(50);
			entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
			entity.Property(o => o.CancelReason).HasMaxLength(500);
			entity.HasIndex(o => o.OrderNo).IsUnique();
			entity.HasIndex(o => o.UserId);
			entity.HasIndex(o => o.Status);
		});

		// OrderItem 实体配置
		modelBuilder.Entity<OrderItem>(entity =>
		{
			entity.ToTable("order_items");
			entity.Property(i => i.ProductName).HasMaxLength(200);
			entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
			entity.HasOne<Domain.Entities.Order>()
				.WithMany(o => o.Items)
				.HasForeignKey(i => i.OrderId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		// SagaInstance 实体配置
		modelBuilder.Entity<SagaInstance>(entity =>
		{
			entity.ToTable("saga_instances");
			entity.Property(s => s.SagaType).HasMaxLength(100);
			entity.Property(s => s.CurrentStep).HasMaxLength(200);
			entity.Property(s => s.LastError).HasColumnType("text");
			entity.HasIndex(s => s.BusinessId);
			entity.HasIndex(s => s.Status);
		});

		// SagaStep 实体配置
		modelBuilder.Entity<SagaStep>(entity =>
		{
			entity.ToTable("saga_steps");
			entity.Property(s => s.StepName).HasMaxLength(200);
			entity.Property(s => s.Status).HasMaxLength(50);
			entity.Property(s => s.RequestJson).HasColumnType("jsonb");
			entity.Property(s => s.ResponseJson).HasColumnType("jsonb");
			entity.Property(s => s.CompensationEvent).HasMaxLength(500);
			entity.HasOne<SagaInstance>()
				.WithMany(s => s.Steps)
				.HasForeignKey(s => s.SagaId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		base.OnModelCreating(modelBuilder);
	}
}
