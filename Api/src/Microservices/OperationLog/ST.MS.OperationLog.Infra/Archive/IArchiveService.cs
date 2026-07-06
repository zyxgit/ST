namespace ST.MS.OperationLog.Infra.Archive;

/// <summary>
/// 操作日志归档服务接口。
/// </summary>
public interface IArchiveService
{
	/// <summary>
	/// 执行归档操作。
	/// </summary>
	/// <param name="cancellation">取消令牌</param>
	/// <returns>归档结果</returns>
	Task<ArchiveResult> ArchiveAsync(CancellationToken cancellation = default);

	/// <summary>
	/// 查询归档数据。
	/// </summary>
	/// <param name="startTime">开始时间</param>
	/// <param name="endTime">结束时间</param>
	/// <param name="cancellation">取消令牌</param>
	/// <returns>归档数据列表</returns>
	Task<List<ArchivedLogEntry>> QueryArchiveAsync(DateTime startTime, DateTime endTime, CancellationToken cancellation = default);
}

/// <summary>
/// 归档结果。
/// </summary>
public sealed class ArchiveResult
{
	/// <summary>是否成功</summary>
	public bool Success { get; set; }

	/// <summary>归档数量</summary>
	public int ArchivedCount { get; set; }

	/// <summary>归档文件路径</summary>
	public string? ArchiveFilePath { get; set; }

	/// <summary>错误信息</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>开始时间</summary>
	public DateTime StartTime { get; set; }

	/// <summary>结束时间</summary>
	public DateTime EndTime { get; set; }
}

/// <summary>
/// 归档日志条目。
/// </summary>
public sealed class ArchivedLogEntry
{
	public DateTime CreatedAtUtc { get; set; }
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
}
