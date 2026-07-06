namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// Inbox 消息存储接口。
/// </summary>
public interface IInboxStore
{
	/// <summary>
	/// 检查指定消息是否已被指定消费者处理过。
	/// </summary>
	Task<bool> ExistsAsync(Guid messageId, string consumer, CancellationToken ct = default);

	/// <summary>
	/// 记录一条 Inbox 消息。
	/// </summary>
	void Add(InboxMessage message);

	/// <summary>
	/// 标记消息已处理完成。
	/// </summary>
	Task MarkAsProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default);

	/// <summary>
	/// 标记消息处理失败，记录错误信息。
	/// </summary>
	Task MarkAsFailedAsync(Guid messageId, string consumer, string error, CancellationToken ct = default);
}
