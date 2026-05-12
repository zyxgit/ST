using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ST.Shared.WebApi.Extensions.OpenApi;

public class EnumXmlCommentDocumentTransformer : IOpenApiDocumentTransformer
{
	Task IOpenApiDocumentTransformer.TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{

		//if (context.?.Type is not { IsEnum: true } enumType)
		//	return Task.CompletedTask;
		////// 已处理过，直接返回（防止重复追加 description）
		////if (ProcessedEnums.ContainsKey(enumType))
		////	return Task.CompletedTask;
		////ProcessedEnums[enumType] = true;

		//var descriptions = new List<string>() { schema.Description ?? string.Empty };

		//if (enumsDescriptions.TryGetValue(enumType, out var enumDescriptions))
		//{
		//	descriptions.AddRange(enumDescriptions);
		//}
		//else
		//{
		//	foreach (var item in Enum.GetValues(enumType))
		//	{
		//		descriptions.Add($"{item} = {(int)item}");
		//	}
		//	enumsDescriptions.TryAdd(enumType, descriptions[1..]);
		//}

		//schema.Description = string.Join("<br/>", descriptions);

		//document.

		return Task.CompletedTask;
	}
}
