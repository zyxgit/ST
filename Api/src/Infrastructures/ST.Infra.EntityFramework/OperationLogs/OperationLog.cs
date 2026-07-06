namespace ST.Infra.EntityFramework.OperationLogs;

public sealed class OperationLog
{
	public long Id { get; set; }

	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

	public string ServiceName { get; set; } = string.Empty;

	public string TraceId { get; set; } = string.Empty;

	public string? SpanId { get; set; }

	public Guid? UserId { get; set; }

	public string? UserName { get; set; }

	public string OperationName { get; set; } = string.Empty;

	public string Path { get; set; } = string.Empty;

	public string Method { get; set; } = string.Empty;

	public string Ip { get; set; } = string.Empty;

	public int StatusCode { get; set; }

	public bool Success { get; set; }

	public long DurationMs { get; set; }

	public string? RequestJson { get; set; }

	public string? ResponseJson { get; set; }

	public string? ExceptionType { get; set; }

	public string? ExceptionMessage { get; set; }

	public string? ExceptionStackTrace { get; set; }

	/// <summary>
	/// 租户 ID
	/// </summary>
	public Guid? TenantId { get; set; }

	/// <summary>
	/// 业务标签（JSON），预留扩展（如业务ID、租户、模块等）。
	/// </summary>
	public string? TagsJson { get; set; }

	/// <summary>
	/// 额外扩展字段（JSON）。
	/// </summary>
	public string? ExtraJson { get; set; }
}

