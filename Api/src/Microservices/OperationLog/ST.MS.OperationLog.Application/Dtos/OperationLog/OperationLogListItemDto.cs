namespace ST.MS.OperationLog.Application.Dtos.OperationLog;

public sealed class OperationLogListItemDto
{
	public long Id { get; init; }

	public DateTime CreatedAtUtc { get; init; }

	public string ServiceName { get; init; } = string.Empty;

	public Guid? UserId { get; init; }

	public string? UserName { get; init; }

	public string OperationName { get; init; } = string.Empty;

	public string Path { get; init; } = string.Empty;

	public string Method { get; init; } = string.Empty;

	public string Ip { get; init; } = string.Empty;

	public int StatusCode { get; init; }

	public bool Success { get; init; }

	public long DurationMs { get; init; }

	public string TraceId { get; init; } = string.Empty;

	public string? ExceptionMessage { get; init; }
}
