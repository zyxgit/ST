namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// Outbox 消息存储接口。
/// </summary>
public interface IOutboxStore
{
	/// <summary>
	/// 添加一条 Outbox 消息。调用方需确保随后调用 SaveChanges 以持久化。
	/// </summary>
	void Add(OutboxMessage message);

	/// <summary>
	/// 批量添加 Outbox 消息。
	/// </summary>
	void AddRange(IEnumerable<OutboxMessage> messages);

	/// <summary>
	/// 查询待发送的 Outbox 消息（状态为 Pending 且已到达重试时间）。
	/// </summary>
	Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);

	/// <summary>
	/// 查询可重试的 Outbox 消息（Pending 或 Failed 且已到达重试时间）。
	/// 供 Outbox Publisher 后台任务使用。
	/// </summary>
	Task<IReadOnlyList<OutboxMessage>> GetRetryableAsync(int batchSize, CancellationToken ct = default);

	/// <summary>
	/// 标记消息已成功发送。
	/// </summary>
	Task MarkAsSentAsync(Guid messageId, CancellationToken ct = default);

	/// <summary>
	/// 标记消息发送失败，记录错误信息并设置下次重试时间。
	/// </summary>
	Task MarkAsFailedAsync(Guid messageId, string error, DateTime nextRetryAtUtc, CancellationToken ct = default);
}
