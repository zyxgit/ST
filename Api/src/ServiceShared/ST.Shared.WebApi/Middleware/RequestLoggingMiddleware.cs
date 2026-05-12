using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Shared.OperationLog;

namespace ST.Shared.WebApi.Middleware;

public class RequestLoggingMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger _requestLogger;
	private readonly RequestLoggingOptions _options;
	private readonly IHostEnvironment _hostEnvironment;

	public RequestLoggingMiddleware(
		RequestDelegate next,
		ILoggerFactory loggerFactory,
		IHostEnvironment hostEnvironment,
		IOptions<RequestLoggingOptions> options)
	{
		_next = next;
		_requestLogger = loggerFactory.CreateLogger("RequestLogger");
		_options = options.Value;
		_hostEnvironment = hostEnvironment;
	}

	public async Task Invoke(HttpContext context)
	{
		var sw = Stopwatch.StartNew();

		string? body = null;
		if (_options.Enabled && _options.LogBody && ShouldLogBody(context))
		{
			// 允许重复读取 Body（仅在确实需要记录时才启用）
			context.Request.EnableBuffering();

			body = await ReadBodyWithLimitAsync(context.Request, _options.MaxBodyChars);
			if (_options.MaskJsonBody && !string.IsNullOrWhiteSpace(body) && IsJsonBody(context.Request))
			{
				body = TryMaskJsonBody(body);
			}
		}

		Exception? exception = null;

		try
		{
			await _next(context);
		}
		catch (Exception ex)
		{
			exception = ex;
			throw;
		}
		finally
		{
			sw.Stop();

			if (_options.Enabled)
			{
				var requestInfo = new
				{
					context.Request.Method,
					Path = context.Request.Path.ToString(),
					Query = context.Request.QueryString.Value,
					Body = body,
					Status = context.Response.StatusCode,
					Elapsed = sw.ElapsedMilliseconds,
					Ip = context.Connection.RemoteIpAddress?.ToString(),
					RequestId = context.TraceIdentifier,
					TraceId = Activity.Current?.TraceId.ToString(),
					Service = _hostEnvironment.ApplicationName,
					ExceptionType = exception?.GetType().FullName,
					ExceptionMessage = exception?.Message
				};

				if (exception is null && context.Response.StatusCode < StatusCodes.Status500InternalServerError)
				{
					_requestLogger.LogInformation("HTTP Request {@Request}", requestInfo);
				}
				else
				{
					_requestLogger.LogError(exception, "HTTP Request Failed {@Request}", requestInfo);
				}
			}
		}
	}

	private bool IsAllowedContentType(HttpRequest request)
	{
		var contentType = request.ContentType ?? string.Empty;
		if (string.IsNullOrWhiteSpace(contentType))
		{
			return false;
		}

		foreach (var allowed in _options.AllowedContentTypes)
		{
			if (contentType.Contains(allowed, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private bool ShouldLogBody(HttpContext context)
	{
		if (context.Request.ContentLength is null || context.Request.ContentLength <= 0)
		{
			return false;
		}

		if (!context.Request.Body.CanRead)
		{
			return false;
		}

		// 通常 GET 不记录 body；仅在 POST/PUT/PATCH/DELETE 等可能有负载时才尝试
		var method = context.Request.Method?.ToUpperInvariant();
		var hasBodyByMethod = method is "POST" or "PUT" or "PATCH" or "DELETE";
		return hasBodyByMethod && IsAllowedContentType(context.Request);
	}

	private static bool IsJsonBody(HttpRequest request)
	{
		var contentType = request.ContentType ?? string.Empty;
		return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase);
	}

	private string? TryMaskJsonBody(string body)
	{
		try
		{
			// 复用 OperationLog 的 JSON 脱敏逻辑（只要 sensitive keys 匹配即可）
			var maskOptions = new OperationLogOptions
			{
				MaskEnabled = _options.MaskJsonBody,
				Mask = _options.Mask,
				SensitiveKeys = _options.SensitiveKeys
			};
			return OperationLogMasker.MaskJson(body, maskOptions);
		}
		catch (JsonException)
		{
			// 非标准 JSON 直接返回原值（但本来就有最大长度限制）
			return body;
		}
		catch
		{
			return body;
		}
	}

	private static async Task<string?> ReadBodyWithLimitAsync(HttpRequest request, int maxChars)
	{
		if (maxChars <= 0)
		{
			return null;
		}

		// 读取前回到开头，避免 Position 不是 0
		if (request.Body.CanSeek)
		{
			request.Body.Position = 0;
		}

		using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

		var sb = new StringBuilder();
		var buffer = new char[4096];
		var total = 0;

		while (total < maxChars)
		{
			var toRead = Math.Min(buffer.Length, maxChars - total);
			var read = await reader.ReadAsync(buffer, 0, toRead);
			if (read <= 0)
			{
				break;
			}

			sb.Append(buffer, 0, read);
			total += read;
		}

		// 读完后把 Position 还原，确保下游仍可读取 Body
		if (request.Body.CanSeek)
		{
			request.Body.Position = 0;
		}

		var text = sb.ToString();
		return string.IsNullOrWhiteSpace(text) ? null : text;
	}
}
