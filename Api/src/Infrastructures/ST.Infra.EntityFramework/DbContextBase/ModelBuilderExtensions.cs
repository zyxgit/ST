using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ST.Infra.Repository.Entities;

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
