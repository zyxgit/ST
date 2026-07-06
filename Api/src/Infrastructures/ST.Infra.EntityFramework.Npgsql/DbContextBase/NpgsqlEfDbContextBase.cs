using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ST.Infra.EntityFramework.DbContextBase;
using ST.Infra.Repository.Entities;
using ST.Infra.Repository.Interface;
using ST.Shared;
using ST.Shared.Security;

namespace ST.Infra.EntityFramework.Npgsql.DbContextBase;

public abstract class NpgsqlEfDbContextBase : EfDbContextBase
{
	protected NpgsqlEfDbContextBase(DbContextOptions options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// 注意：这里原先有一段把 DateTime 统一映射为 PostgreSQL 的 timestamp（不带时区）的逻辑。
		// 现在按你的要求先注释掉，保持迁移/模型默认行为（Npgsql 通常会映射为 timestamp with time zone）。
		// 如果后续要彻底切换成 timestamp（不带时区），建议通过明确的迁移来改列类型。

		//foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		//{
		//	foreach (var property in entityType.GetProperties())
		//	{
		//		if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
		//		{
		//			property.SetColumnType("timestamp");
		//		}
		//	}
		//}
	}

	public override int SaveChanges(bool acceptAllChangesOnSuccess)
	{
		SyncTenantContext();
		FillAuditFields();
		NormalizeDateTimeToUtc();
		return base.SaveChanges(acceptAllChangesOnSuccess);
	}

	public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
	{
		SyncTenantContext();
		FillAuditFields();
		NormalizeDateTimeToUtc();
		return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
	}

	/// <summary>
	/// 从 ICurrentTenantAccessor 同步租户 ID 到 TenantContext，
	/// 确保查询过滤器和自动填充使用正确的租户上下文。
	/// </summary>
	private void SyncTenantContext()
	{
		if (TenantContext.CurrentTenantId.HasValue)
			return;

		var tenantAccessor = this.GetService<ICurrentTenantAccessor>();
		if (tenantAccessor?.TenantId is { } tid)
		{
			TenantContext.CurrentTenantId = tid;
		}
	}

	private void FillAuditFields()
	{
		var nowUtc = DateTime.UtcNow;
		var userId = GetCurrentUserIdOrUnknown();

		foreach (var entry in ChangeTracker.Entries())
		{
			if (entry.State == EntityState.Added)
			{
				if (entry.Entity is IBasicAuditInfo basic)
				{
					basic.CreateBy = userId;
					basic.CreateTime = nowUtc;
				}

				if (entry.Entity is IFullAuditInfo full)
				{
					full.ModifyBy = userId;
					full.ModifyTime = nowUtc;
				}

				// 自动填充租户 ID
				if (entry.Entity is ITenantEntity tenant && tenant.TenantId == Guid.Empty
					&& TenantContext.CurrentTenantId.HasValue)
				{
					tenant.TenantId = TenantContext.CurrentTenantId.Value;
				}
			}
			else if (entry.State == EntityState.Modified)
			{
				if (entry.Entity is IFullAuditInfo full)
				{
					full.ModifyBy = userId;
					full.ModifyTime = nowUtc;
				}
			}
		}
	}

	private Guid GetCurrentUserIdOrUnknown()
	{
		// 未登录时用 Guid.Empty 表示 unknown
		var accessor = this.GetService<ICurrentUserIdAccessor>();
		return accessor?.UserId ?? Guid.Empty;
	}

	private void NormalizeDateTimeToUtc()
	{
		foreach (var entry in ChangeTracker.Entries())
		{
			if (entry.State != EntityState.Added && entry.State != EntityState.Modified)
			{
				continue;
			}

			foreach (var property in entry.Properties)
			{
				ConvertDateTimePropertyToUtc(property);
			}
		}
	}

	private static void ConvertDateTimePropertyToUtc(PropertyEntry property)
	{
		if (property.Metadata.ClrType == typeof(DateTime))
		{
			var value = (DateTime)property.CurrentValue!;
			property.CurrentValue = EnsureUtc(value);
			return;
		}

		if (property.Metadata.ClrType == typeof(DateTime?))
		{
			var value = (DateTime?)property.CurrentValue;
			if (value.HasValue)
			{
				property.CurrentValue = EnsureUtc(value.Value);
			}
		}
	}

	private static DateTime EnsureUtc(DateTime value)
	{
		return value.Kind switch
		{
			DateTimeKind.Utc => value,
			DateTimeKind.Local => value.ToUniversalTime(),
			// Unspecified：为了避免 Npgsql 写入 timestamptz 抛错，这里直接标记为 UTC。
			DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
			_ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
		};
	}
}
