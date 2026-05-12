using ST.Infra.EntityFramework.Npgsql.DbContextBase;
using ST.Infra.EntityFramework.OperationLogs;

namespace ST.MS.OperationLog.Infra.DbContext;

public sealed class OperationLogDbContext : NpgsqlEfDbContextBase
{
	public DbSet<ST.Infra.EntityFramework.OperationLogs.OperationLog> OperationLogs { get; set; }

	public OperationLogDbContext(DbContextOptions options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// 先配置操作日志模型（包含 jsonb/text），避免被默认字符串长度约束截断
		modelBuilder.ApplyOperationLogs();
		base.OnModelCreating(modelBuilder);
	}
}
