using ST.Shared.Application.Dtos;

namespace ST.MS.OperationLog.Application.Dtos.OperationLog;

public sealed class OperationLogQueryInputDto : PagedRequestDto
{
	public string? ServiceName { get; set; }

	public Guid? UserId { get; set; }

	public string? TraceId { get; set; }

	public string? Method { get; set; }

	public string? Path { get; set; }

	public string? OperationName { get; set; }

	public bool? Success { get; set; }

	public int? StatusCode { get; set; }

	public string? Keyword { get; set; }

	public DateTime? StartTimeUtc { get; set; }

	public DateTime? EndTimeUtc { get; set; }
}
