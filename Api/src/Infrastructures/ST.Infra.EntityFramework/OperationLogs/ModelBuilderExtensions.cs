using Microsoft.EntityFrameworkCore;

namespace ST.Infra.EntityFramework.OperationLogs;

public static class ModelBuilderExtensions
{
	public static ModelBuilder ApplyOperationLogs(this ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<OperationLog>(entity =>
		{
			entity.ToTable("operation_logs");
			entity.HasKey(x => x.Id);

			entity.HasIndex(x => x.CreatedAtUtc);
			entity.HasIndex(x => x.UserId);
			entity.HasIndex(x => x.OperationName);
			entity.HasIndex(x => x.TraceId);

			entity.Property(x => x.ServiceName).HasMaxLength(200);
			entity.Property(x => x.TraceId).HasMaxLength(64);
			entity.Property(x => x.SpanId).HasMaxLength(64);
			entity.Property(x => x.UserName).HasMaxLength(200);

			entity.Property(x => x.OperationName).HasMaxLength(200);
			entity.Property(x => x.Path).HasMaxLength(512);
			entity.Property(x => x.Method).HasMaxLength(16);
			entity.Property(x => x.Ip).HasMaxLength(64);

			// 长文本/JSON：显式声明类型，避免被默认字符串长度约束截断
			entity.Property(x => x.RequestJson).HasColumnType("jsonb");
			entity.Property(x => x.ResponseJson).HasColumnType("jsonb");
			entity.Property(x => x.TagsJson).HasColumnType("jsonb");
			entity.Property(x => x.ExtraJson).HasColumnType("jsonb");

			entity.Property(x => x.ExceptionType).HasMaxLength(300);
			entity.Property(x => x.ExceptionMessage).HasColumnType("text");
			entity.Property(x => x.ExceptionStackTrace).HasColumnType("text");
		});

		return modelBuilder;
	}
}
