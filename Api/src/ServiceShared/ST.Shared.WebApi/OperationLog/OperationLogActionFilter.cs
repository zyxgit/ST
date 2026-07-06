using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Shared.Attributes;
using ST.Shared.OperationLog;
using ST.Shared.Security;

namespace ST.Shared.WebApi.OperationLog;

public sealed class OperationLogActionFilter : IAsyncActionFilter
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private readonly IOperationLogDispatcher _dispatcher;
	private readonly IUserContext _userContext;
	private readonly IOptions<OperationLogOptions> _options;
	private readonly ILogger<OperationLogActionFilter> _logger;
	private readonly IHostEnvironment _hostEnvironment;

	public OperationLogActionFilter(
		IOperationLogDispatcher dispatcher,
		IUserContext userContext,
		IOptions<OperationLogOptions> options,
		ILogger<OperationLogActionFilter> logger,
		IHostEnvironment hostEnvironment)
	{
		_dispatcher = dispatcher;
		_userContext = userContext;
		_options = options;
		_logger = logger;
		_hostEnvironment = hostEnvironment;
	}

	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		var attr = context.ActionDescriptor.EndpointMetadata.OfType<OperationLogAttribute>().FirstOrDefault();
		if (attr is null)
		{
			await next();
			return;
		}

		var opt = _options.Value;
		if (!opt.Enabled)
		{
			await next();
			return;
		}

		if (opt.SampleRate < 1.0 && Random.Shared.NextDouble() > opt.SampleRate)
		{
			await next();
			return;
		}

		var http = context.HttpContext;
		var sw = Stopwatch.StartNew();

		string? requestJson = null;
		if (attr.RecordRequest)
		{
			requestJson = SerializeAndMask(context.ActionArguments, opt, GetMaxLen(opt, attr));
		}

		ActionExecutedContext? executed = null;
		Exception? exception = null;
		object? responseObj = null;

		try
		{
			executed = await next();
			exception = executed.Exception;

			responseObj = executed.Result switch
			{
				ObjectResult o => o.Value,
				JsonResult j => j.Value,
				_ => null
			};
		}
		catch (Exception ex)
		{
			exception = ex;
			throw;
		}
		finally
		{
			sw.Stop();

			var recordResp = attr.RecordResponse || opt.RecordResponseByDefault;
			string? responseJson = null;
			if (recordResp)
			{
				responseJson = SerializeAndMask(responseObj, opt, GetMaxLen(opt, attr));
			}

			var statusCode = exception is null ? http.Response.StatusCode : StatusCodes.Status500InternalServerError;
			var success = exception is null && statusCode is >= 200 and < 400;

			var activity = Activity.Current;
			var entry = new OperationLogEntry
			{
				OccurredOnUtc = DateTimeOffset.UtcNow,
				ServiceName = _hostEnvironment.ApplicationName ?? string.Empty,
				TraceId = activity?.TraceId.ToString() ?? http.TraceIdentifier,
				SpanId = activity?.SpanId.ToString(),
				UserId = _userContext.UserId,
				TenantId = _userContext.TenantId,
				UserName = _userContext.NickName ?? _userContext.Email,
				OperationName = attr.OperationName,
				Path = http.Request.Path.Value ?? string.Empty,
				Method = http.Request.Method ?? string.Empty,
				Ip = _userContext.ClientIp ?? http.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
				StatusCode = statusCode,
				Success = success,
				DurationMs = sw.ElapsedMilliseconds,
				RequestJson = requestJson,
				ResponseJson = responseJson,
				ExceptionType = exception?.GetType().FullName,
				ExceptionMessage = exception?.Message,
				ExceptionStackTrace = Truncate(exception?.StackTrace, 8_192)
			};

			try
			{
				await _dispatcher.EnqueueAsync(entry);
			}
			catch (Exception enqueueEx)
			{
				_logger.LogError(enqueueEx, "OperationLog enqueue failed. TraceId={TraceId}", entry.TraceId);
			}
		}
	}

	private static int GetMaxLen(OperationLogOptions options, OperationLogAttribute attr)
	{
		if (attr.MaxBodyLength > 0)
		{
			return attr.MaxBodyLength;
		}

		return options.MaxBodyLength;
	}

	private static string? SerializeAndMask(object? obj, OperationLogOptions options, int maxLen)
	{
		if (obj is null)
		{
			return null;
		}

		if (obj is IFormFile or IFormFileCollection)
		{
			return "\"<file>\"";
		}

		var json = JsonSerializer.Serialize(obj, JsonOptions);
		if (options.MaskEnabled)
		{
			json = OperationLogMasker.MaskJson(json, options);
		}

		return Truncate(json, maxLen);
	}

	private static string? Truncate(string? text, int maxLen)
	{
		if (string.IsNullOrEmpty(text) || maxLen <= 0 || text.Length <= maxLen)
		{
			return text;
		}

		return text[..maxLen];
	}
}
