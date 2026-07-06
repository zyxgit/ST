namespace ST.Infra.ReliableMessaging.Models;

/// <summary>
/// Inbox 消息实体。
/// 消费端基于 MessageId + Consumer 做幂等去重，
/// 防止重复投递导致业务状态混乱。
/// </summary>
public sealed class InboxMessage
{
	/// <summary>记录 ID（主键）</summary>
	public Guid Id { get; set; } = Guid.CreateVersion7();

	/// <summary>消息 ID（来自 IntegrationEvent.Id），与 Consumer 构成唯一约束</summary>
	public Guid MessageId { get; set; }

	/// <summary>消费者标识（如服务名 + Handler 名），与 MessageId 构成唯一约束</summary>
	public string Consumer { get; set; } = string.Empty;

	/// <summary>集成事件类型全名</summary>
	public string EventType { get; set; } = string.Empty;

	/// <summary>消息接收时间（UTC）</summary>
	public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

	/// <summary>处理完成时间（UTC），null 表示尚未处理</summary>
	public DateTime? ProcessedAtUtc { get; set; }

	/// <summary>处理失败时的错误信息</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>已重试次数</summary>
	public int RetryCount { get; set; }
}
