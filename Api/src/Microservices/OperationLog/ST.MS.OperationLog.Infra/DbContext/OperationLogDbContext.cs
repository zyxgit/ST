using ST.Infra.EntityFramework.Npgsql.DbContextBase;
using ST.Infra.EntityFramework.OperationLogs;
using ST.MS.OperationLog.Infra.Entities;

namespace ST.MS.OperationLog.Infra.DbContext;

public sealed class OperationLogDbContext : NpgsqlEfDbContextBase
{
	public DbSet<ST.Infra.EntityFramework.OperationLogs.OperationLog> OperationLogs { get; set; }
	public DbSet<DeadLetterMessage> DeadLetterMessages { get; set; }

	public OperationLogDbContext(DbContextOptions options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// 先配置操作日志模型（包含 jsonb/text），避免被默认字符串长度约束截断
		modelBuilder.ApplyOperationLogs();

		// 配置死信消息模型
		modelBuilder.Entity<DeadLetterMessage>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.OriginalMessage).HasColumnType("jsonb");
			entity.Property(e => e.QueueName).HasMaxLength(200);
			entity.Property(e => e.ExchangeName).HasMaxLength(200);
			entity.Property(e => e.RoutingKey).HasMaxLength(200);
			entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
			entity.Property(e => e.ErrorStackTrace).HasMaxLength(10000);
			entity.Property(e => e.ReplayResult).HasMaxLength(500);
			entity.HasIndex(e => e.CreatedAtUtc);
			entity.HasIndex(e => new { e.QueueName, e.CreatedAtUtc });
		});

		base.OnModelCreating(modelBuilder);
	}
}
