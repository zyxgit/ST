namespace ST.Infra.EntityFramework.Extensions;

public static class ModelBuilderCommentExtensions
{
	//public static void ApplyXmlComments(
	//   this ModelBuilder modelBuilder,
	//   Assembly entityAssembly)
	//{
	//	foreach (var entity in modelBuilder.Model.GetEntityTypes())
	//	{
	//		var clrType = entity.ClrType;

	//		if (clrType.Assembly != entityAssembly)
	//			continue;

	//		var tableComment = XmlCommentReader.GetTypeComment(clrType);
	//		if (!string.IsNullOrWhiteSpace(tableComment))
	//		{
	//			entity.SetComment(tableComment);
	//		}

	//		foreach (var prop in clrType.GetProperties())
	//		{
	//			var comment = XmlCommentReader.GetPropertyComment(prop);
	//			if (!string.IsNullOrWhiteSpace(comment))
	//			{
	//				entity.FindProperty(prop.Name)?.SetComment(comment);
	//			}
	//		}
	//	}
	//}
}
