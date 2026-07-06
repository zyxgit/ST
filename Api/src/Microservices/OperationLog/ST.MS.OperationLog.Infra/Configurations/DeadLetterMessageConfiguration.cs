using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.MS.OperationLog.Infra.Entities;

namespace ST.MS.OperationLog.Infra.Configurations;

/// <summary>
/// DeadLetterMessage 实体配置（消除 IEntityTypeConfiguration 扫描警告）
/// </summary>
public sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
	public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
	{
		// 实际配置已在 OperationLogDbContext.OnModelCreating 中完成
		// 此类仅用于消除程序集扫描警告
	}
}
