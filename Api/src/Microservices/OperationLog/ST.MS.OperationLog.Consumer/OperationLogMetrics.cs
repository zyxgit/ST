using System.Diagnostics.Metrics;

namespace ST.MS.OperationLog.Consumer;

/// <summary>
/// OperationLog Consumer 自定义 OpenTelemetry 指标。
/// Meter 名称：ST.OperationLog.Consumer
/// </summary>
public static class OperationLogMetrics
{
	public static readonly Meter Meter = new("ST.OperationLog.Consumer", "1.0.0");

	// ── 计数器 ────────────────────────────────────────────────────────────────

	/// <summary>接收到的消息总数</summary>
	public static readonly Counter<long> MessagesReceived =
		Meter.CreateCounter<long>("st.operationlog.messages.received", description: "接收到的消息总数");

	/// <summary>批量写入成功数</summary>
	public static readonly Counter<long> BatchWriteSuccess =
		Meter.CreateCounter<long>("st.operationlog.batch.write.success", description: "批量写入成功数");

	/// <summary>单条降级写入成功数</summary>
	public static readonly Counter<long> SingleWriteSuccess =
		Meter.CreateCounter<long>("st.operationlog.single.write.success", description: "单条降级写入成功数");

	/// <summary>写入失败总数</summary>
	public static readonly Counter<long> WriteFailed =
		Meter.CreateCounter<long>("st.operationlog.write.failed", description: "写入失败总数");

	/// <summary>写入死信表数</summary>
	public static readonly Counter<long> DeadLetterWritten =
		Meter.CreateCounter<long>("st.operationlog.deadletter.written", description: "写入死信表数");

	/// <summary>死信重放成功数</summary>
	public static readonly Counter<long> DeadLetterReplaySuccess =
		Meter.CreateCounter<long>("st.operationlog.deadletter.replay.success", description: "死信重放成功数");

	/// <summary>死信重放失败数</summary>
	public static readonly Counter<long> DeadLetterReplayFailed =
		Meter.CreateCounter<long>("st.operationlog.deadletter.replay.failed", description: "死信重放失败数");

	/// <summary>归档日志条数</summary>
	public static readonly Counter<long> ArchiveCount =
		Meter.CreateCounter<long>("st.operationlog.archive.count", description: "归档日志条数");

	/// <summary>归档失败次数</summary>
	public static readonly Counter<long> ArchiveFailed =
		Meter.CreateCounter<long>("st.operationlog.archive.failed", description: "归档失败次数");

	// ── 直方图 ────────────────────────────────────────────────────────────────

	/// <summary>批量写入条数分布</summary>
	public static readonly Histogram<double> BatchSize =
		Meter.CreateHistogram<double>("st.operationlog.batch.size", description: "批量写入条数分布");

	/// <summary>刷新耗时 (ms)</summary>
	public static readonly Histogram<double> FlushDurationMs =
		Meter.CreateHistogram<double>("st.operationlog.flush.duration_ms", description: "刷新耗时(ms)");
}
