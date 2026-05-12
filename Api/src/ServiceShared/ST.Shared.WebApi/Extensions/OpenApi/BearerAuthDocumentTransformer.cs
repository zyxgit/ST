using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
//using Microsoft.OpenApi;

namespace ST.Shared.WebApi.Extensions.OpenApi;

/// <summary>
/// 在 OpenAPI 文档中声明 JWT Bearer 认证方案，便于 Scalar/Swagger 识别并注入 Authorization 头。
/// </summary>
public sealed class BearerAuthDocumentTransformer : IOpenApiDocumentTransformer
{
	public const string SchemeName = "BearerAuth";

	public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
	{
		// 确保 Components 存在
		document.Components ??= new OpenApiComponents();

		// 将 SecuritySchemes 初始化为能接受接口类型的字典，避免 Dictionary<string, OpenApiSecurityScheme> 无法隐式转换为 IDictionary<string, IOpenApiSecurityScheme> 的错误
		var schemes = document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

		// 只有在不存在同名 scheme 时才添加
		if (!schemes.ContainsKey(SchemeName))
		{
			schemes[SchemeName] = new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT",
				Description = "在请求头中添加：Authorization: Bearer <token>"
			};
		}

		return Task.CompletedTask;
	}
}
