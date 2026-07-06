using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ST.Infra.Repository.Entities;
using ST.Shared;

namespace ST.Infra.EntityFramework.DbContextBase;

public static class ModelBuilderExtensions
{
	/// <summary>
	/// 软删除过滤器
	/// </summary>
	/// <param name="modelBuilder"></param>
	public static void ApplySoftDeleteQueryFilter(this ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
			{
				var parameter = Expression.Parameter(entityType.ClrType, "e");

				var propertyMethod = typeof(EF)
					.GetMethod(nameof(EF.Property))!
					.MakeGenericMethod(typeof(bool));

				var isDeletedProperty = Expression.Call(
					propertyMethod,
					parameter,
					Expression.Constant(nameof(ISoftDelete.IsDeleted)));

				var compareExpression = Expression.Equal(
					isDeletedProperty,
					Expression.Constant(false));

				var lambda = Expression.Lambda(compareExpression, parameter);

				modelBuilder.Entity(entityType.ClrType)
					.HasQueryFilter(lambda);
			}
		}
	}

	/// <summary>
	/// 租户数据隔离过滤器。
	/// 对实现 ITenantEntity 的实体自动附加 WHERE tenant_id = @currentTenantId。
	/// 当 TenantContext.CurrentTenantId 为 null 时不过滤（超级管理员/后台任务场景）。
	/// 若实体同时实现 ISoftDelete，则与软删除过滤器合并为 AND。
	/// </summary>
	public static void ApplyTenantQueryFilter(this ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (!typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
				continue;

			var parameter = Expression.Parameter(entityType.ClrType, "e");

			// e.TenantId
			var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

			// TenantContext.CurrentTenantId
			var currentTenantId = Expression.Property(null, typeof(TenantContext), nameof(TenantContext.CurrentTenantId));

			// e.TenantId == (Guid)TenantContext.CurrentTenantId
			// TenantId 是 Guid（非空），CurrentTenantId 是 Guid?（可空），需要类型转换
			var tenantIdAsNullable = Expression.Convert(tenantIdProperty, typeof(Guid?));
			var tenantMatch = Expression.Equal(tenantIdAsNullable, currentTenantId);

			// TenantContext.CurrentTenantId == null （null 时不过滤）
			var tenantIsNull = Expression.Equal(currentTenantId, Expression.Constant(null, typeof(Guid?)));

			// TenantContext.CurrentTenantId == null || e.TenantId == TenantContext.CurrentTenantId
			var combined = Expression.OrElse(tenantIsNull, tenantMatch);

			// 如果实体同时有 ISoftDelete，合并已有过滤器
			if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
			{
				// e.IsDeleted == false
				var propertyMethod = typeof(EF)
					.GetMethod(nameof(EF.Property))!
					.MakeGenericMethod(typeof(bool));

				var isDeletedProperty = Expression.Call(
					propertyMethod,
					parameter,
					Expression.Constant(nameof(ISoftDelete.IsDeleted)));

				var softDeleteFilter = Expression.Equal(isDeletedProperty, Expression.Constant(false));

				// (e.IsDeleted == false) && (tenantId == null || e.TenantId == tenantId)
				combined = Expression.AndAlso(softDeleteFilter, combined);
			}

			var lambda = Expression.Lambda(combined, parameter);

			modelBuilder.Entity(entityType.ClrType)
				.HasQueryFilter(lambda);
		}
	}

	/// <summary>
	/// 移除模型中所有外键关系，使建表/迁移不生成 FOREIGN KEY 约束。
	/// 与 NoForeignKeySqlGenerator 配合，从模型层和 SQL 层双保险禁止外键。
	/// </summary>
	public static void ApplyNoForeignKeys(this ModelBuilder modelBuilder)
	{
		var mutableModel = (IMutableModel)modelBuilder.Model;
		foreach (var entityType in mutableModel.GetEntityTypes().ToList())
		{
			foreach (var fk in entityType.GetForeignKeys().ToList())
			{
				entityType.RemoveForeignKey(fk);
			}
		}
	}

	/// <summary>
	/// 默认字符串最大长度
	/// </summary>
	/// <param name="modelBuilder"></param>
	/// <param name="defaultLength"></param>
	public static void ApplyDefaultStringLength(this ModelBuilder modelBuilder, int defaultLength = 200)
	{
		foreach (var property in modelBuilder.Model
			.GetEntityTypes()
			.SelectMany(e => e.GetProperties())
			.Where(p =>
				p.ClrType == typeof(string) &&
				p.GetMaxLength() == null &&
				!IsUnlimitedTextOrJsonColumn(p)))
		{
			property.SetMaxLength(defaultLength);
		}
	}

	private static bool IsUnlimitedTextOrJsonColumn(IMutableProperty property)
	{
		var columnType = property.GetColumnType();
		if (string.IsNullOrWhiteSpace(columnType))
		{
			return false;
		}

		// 常见无限长类型：PostgreSQL text / jsonb / json
		return columnType.Equals("text", StringComparison.OrdinalIgnoreCase)
			|| columnType.Equals("jsonb", StringComparison.OrdinalIgnoreCase)
			|| columnType.Equals("json", StringComparison.OrdinalIgnoreCase);
	}

	#region 生成

	#endregion
	public static void ApplyCommentsFromAttributeOrXml(
		this ModelBuilder modelBuilder,
		Assembly assembly)
	{
		var xml = LoadXml(assembly);

		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			var clrType = entityType.ClrType;
			if (clrType == null || clrType.Assembly != assembly)
				continue;

			// ===== 表注释 =====
			ApplyTableComment(entityType, clrType, xml);

			// ===== 列注释 =====
			foreach (var property in entityType.GetProperties())
			{
				var propInfo = clrType.GetProperty(property.Name);
				if (propInfo == null) continue;

				// 1️⃣ 优先读取特性
				var attr = propInfo.GetCustomAttribute<CommentAttribute>();
				if (attr != null)
				{
					property.SetComment(attr.Comment);
					continue;
				}

				// 2️⃣ fallback 到 XML summary
				var xmlSummary = GetXmlSummary(xml,
					$"P:{clrType.FullName}.{property.Name}");

				if (!string.IsNullOrWhiteSpace(xmlSummary))
				{
					property.SetComment(xmlSummary);
				}
			}
		}
	}

	private static void ApplyTableComment(
		IMutableEntityType entityType,
		Type clrType,
		XDocument? xml)
	{
		// 1️⃣ 优先读取特性
		var attr = clrType.GetCustomAttribute<CommentAttribute>();
		if (attr != null)
		{
			entityType.SetComment(attr.Comment);
			return;
		}

		// 2️⃣ fallback 到 XML summary
		var xmlSummary = GetXmlSummary(xml,
			$"T:{clrType.FullName}");

		if (!string.IsNullOrWhiteSpace(xmlSummary))
		{
			entityType.SetComment(xmlSummary);
		}
	}

	private static XDocument? LoadXml(Assembly assembly)
	{
		var xmlPath = Path.Combine(
			AppContext.BaseDirectory,
			$"{assembly.GetName().Name}.xml");

		if (!File.Exists(xmlPath))
			return null;

		return XDocument.Load(xmlPath);
	}

	private static string? GetXmlSummary(
		XDocument? xml,
		string memberName)
	{
		if (xml == null) return null;

		return xml.Descendants("member")
			.FirstOrDefault(x =>
				x.Attribute("name")?.Value == memberName)?
			.Element("summary")?
			.Value?
			.Trim();
	}

}
