using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ST.Shared.Const;
using ST.Shared.Exceptions;

namespace ST.Shared.WebApi.Middleware;

public class GlobalExceptionMiddleware
{
	private readonly RequestDelegate _next;
	private readonly IConfiguration _configuration;
	private readonly ILogger<GlobalExceptionMiddleware> _logger;

	public GlobalExceptionMiddleware(
		RequestDelegate next,
		IConfiguration configuration,
		ILogger<GlobalExceptionMiddleware> logger)
	{
		_next = next;
		_configuration = configuration;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		try
		{
			await _next(context);
		}
		catch (BusinessException ex)
		{
			//_logger.LogWarning(ex, ex.Message);
			await WriteProblemAsync(context, ex.StatusCode, "业务异常", ex.Message, ex.ErrorCode);
		}
		catch (DomainException ex)
		{
			//_logger.LogWarning(ex, ex.Message);
			await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "验证失败", ex.Message);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "【未处理的异常】");
			await WriteProblemAsync(
				context,
				StatusCodes.Status500InternalServerError,
				"请求异常",
				_configuration.GetValue<string>(SettingPrefixContants.App_ErrorMessage) ?? DefaultContants.ErrorMessage
				);
		}
	}

	/// <summary>
	/// 错误返回处理
	/// </summary>
	/// <param name="context"></param>
	/// <param name="statusCode"></param>
	/// <param name="title"></param>
	/// <param name="detail"></param>
	/// <param name="errorCode"></param>
	/// <returns></returns>
	private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail, string? errorCode = null)
	{
		context.Response.StatusCode = statusCode;
		context.Response.ContentType = "application/problem+json";

		var problem = new ProblemDetails
		{
			Status = statusCode,
			Title = title,
			Detail = detail,
			Instance = context.Request.Path + context.Request.QueryString
		};

		problem.Extensions["traceId"] = context.TraceIdentifier;

		if (!string.IsNullOrWhiteSpace(errorCode))
		{
			problem.Extensions["errorCode"] = errorCode;
		}

		await context.Response.WriteAsJsonAsync(problem);
	}
}
