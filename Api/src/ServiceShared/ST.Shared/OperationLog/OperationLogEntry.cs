namespace ST.Shared.OperationLog;

public sealed record OperationLogEntry
{
	public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

	public string ServiceName { get; init; } = string.Empty;

	public string TraceId { get; init; } = string.Empty;

	public string? SpanId { get; init; }

	public Guid? UserId { get; init; }

	public string? UserName { get; init; }

	public string OperationName { get; init; } = string.Empty;

	public string Path { get; init; } = string.Empty;

	public string Method { get; init; } = string.Empty;

	public string Ip { get; init; } = string.Empty;

	public int StatusCode { get; init; }

	public bool Success { get; init; }

	public long DurationMs { get; init; }

	public string? RequestJson { get; init; }

	public string? ResponseJson { get; init; }

	public string? ExceptionType { get; init; }

	public string? ExceptionMessage { get; init; }

	public string? ExceptionStackTrace { get; init; }
}

