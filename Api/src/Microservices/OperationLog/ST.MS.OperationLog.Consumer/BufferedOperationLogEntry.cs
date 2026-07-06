using ST.Shared.OperationLog;

namespace ST.MS.OperationLog.Consumer;

/// <summary>
/// 缓冲区中的操作日志条目。
/// 包装 OperationLogEntry 并附加 RabbitMQ DeliveryTag 用于后续 ack/nack。
/// </summary>
internal sealed class BufferedOperationLogEntry
{
	/// <summary>操作日志条目</summary>
	public required OperationLogEntry Entry { get; init; }

	/// <summary>RabbitMQ Delivery Tag</summary>
	public ulong DeliveryTag { get; init; }
}
