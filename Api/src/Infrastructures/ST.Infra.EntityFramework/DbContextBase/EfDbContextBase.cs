using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ST.Infra.Repository.Entities;
using ST.Infra.Repository.Interface;
using ST.Shared;
using ST.Shared.Security;

namespace ST.Infra.EntityFramework.DbContextBase;

public abstract class EfDbContextBase : DbContext
{
	protected EfDbContextBase(DbContextOptions options) : base(options)
	{

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

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
		//默认未设置字符串长度
		modelBuilder.ApplyDefaultStringLength();
		//软删除过滤器
		modelBuilder.ApplySoftDeleteQueryFilter();
		//租户数据隔离过滤器（必须在软删除之后，合并两者）
		modelBuilder.ApplyTenantQueryFilter();
		// 注意：不在运行时移除外键关系（ApplyNoForeignKeys），否则 Include/ThenInclude 导航属性
		// 会丢失 JOIN 条件元数据，导致查询退化为笛卡尔积或全表扫描。
		// 外键禁止已由 NoForeignKeySqlGenerator 在迁移 SQL 层面实现，运行时保留模型关系即可。
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseSnakeCaseNamingConvention();
		// 抑制模型待变更警告（CodeFirst 迁移场景下模型与快照可能不一致）。
		optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
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
