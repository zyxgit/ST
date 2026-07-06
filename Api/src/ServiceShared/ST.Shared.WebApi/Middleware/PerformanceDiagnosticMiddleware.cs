using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ST.Shared.WebApi.Middleware;

/// <summary>
/// 性能诊断中间件（仅 Development 环境启用）。
/// 在请求管线的最外层测量各阶段耗时，定位慢请求瓶颈。
/// </summary>
public sealed class PerformanceDiagnosticMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<PerformanceDiagnosticMiddleware> _logger;

	public PerformanceDiagnosticMiddleware(RequestDelegate next, ILogger<PerformanceDiagnosticMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		var totalSw = Stopwatch.StartNew();
		var stepSw = new Stopwatch();

		// ── 1. 请求读取阶段 ──
		stepSw.Restart();
		// 请求体读取（如果有）
		var requestBodyBytes = 0L;
		if (context.Request.ContentLength > 0)
		{
			context.Request.EnableBuffering();
			requestBodyBytes = context.Request.ContentLength.Value;
			context.Request.Body.Position = 0;
		}
		stepSw.Stop();
		var readMs = stepSw.ElapsedMilliseconds;

		// ── 2. 认证阶段（Authentication + Authorization）──
		// 这两个在 _next 内部由 UseAuthentication/UseAuthorization 处理，
		// 我们通过 Activity 来追踪

		// ── 3. 执行下游管线 ──
		stepSw.Restart();
		try
		{
			await _next(context);
		}
		finally
		{
			stepSw.Stop();
			var nextMs = stepSw.ElapsedMilliseconds;
			totalSw.Stop();
			var totalMs = totalSw.ElapsedMilliseconds;

			// 计算框架开销（总时间 - _next 时间）
			var overheadMs = totalMs - nextMs;

			// OTel Activity 信息
			var activity = Activity.Current;
			var traceId = activity?.TraceId.ToString() ?? "none";
			var spanId = activity?.SpanId.ToString() ?? "none";

			_logger.LogInformation(
				"[PERF] {Method} {Path}{Query} | Status={Status} | " +
				"Total={TotalMs}ms | Pipeline={NextMs}ms | Overhead={OverheadMs}ms | " +
				"Body={BodyBytes}B | TraceId={TraceId}",
				context.Request.Method,
				context.Request.Path,
				context.Request.QueryString,
				context.Response.StatusCode,
				totalMs,
				nextMs,
				overheadMs,
				requestBodyBytes,
				traceId);

			// 如果总时间超过 500ms，标记为慢请求并输出详细信息
			if (totalMs > 500)
			{
				_logger.LogWarning(
					"[PERF-SLOW] {Method} {Path}{Query} took {TotalMs}ms! " +
					"Pipeline={NextMs}ms, FrameworkOverhead={OverheadMs}ms, " +
					"Status={Status}, TraceId={TraceId}, SpanId={SpanId}",
					context.Request.Method,
					context.Request.Path,
					context.Request.QueryString,
					totalMs,
					nextMs,
					overheadMs,
					context.Response.StatusCode,
					traceId,
					spanId);
			}

			Console.WriteLine(
				$"[PERF] {context.Request.Method} {context.Request.Path} | " +
				$"Total={totalMs}ms Pipeline={nextMs}ms Overhead={overheadMs}ms Status={context.Response.StatusCode}");
		}
	}
}
