using System.Collections.Concurrent;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ST.Shared.WebApi.Extensions.OpenApi;

public class EnumXmlCommentSchemaTransformer : IOpenApiSchemaTransformer
{
	public static ConcurrentDictionary<Type, List<string>> enumsDescriptions = new();
	public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
	{
		if (context.JsonTypeInfo?.Type is not { IsEnum: true } enumType)
			return Task.CompletedTask;
		//// 已处理过，直接返回（防止重复追加 description）
		//if (ProcessedEnums.ContainsKey(enumType))
		//	return Task.CompletedTask;
		//ProcessedEnums[enumType] = true;

		var descriptions = new List<string>() { schema.Description ?? string.Empty };

		if (enumsDescriptions.TryGetValue(enumType, out var enumDescriptions))
		{
			descriptions.AddRange(enumDescriptions);
		}
		else
		{
			foreach (var item in Enum.GetValues(enumType))
			{
				descriptions.Add($"{item} = {(int)item}");
			}
			enumsDescriptions.TryAdd(enumType, descriptions[1..]);
		}

		schema.Description = string.Join("<br/>", descriptions);

		return Task.CompletedTask;
	}

}
