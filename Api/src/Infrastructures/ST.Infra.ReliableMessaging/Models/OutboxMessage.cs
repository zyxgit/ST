namespace ST.Infra.ReliableMessaging.Models;

/// <summary>
/// Outbox 消息实体。
/// 业务数据与 Outbox 消息必须在同一个本地数据库事务内提交，
/// 由 Outbox Publisher 后台任务扫描并通过消息代理投递。
/// </summary>
public sealed class OutboxMessage
{
	/// <summary>消息 ID（主键）</summary>
	public Guid Id { get; set; } = Guid.CreateVersion7();

	/// <summary>聚合根 ID，用于关联业务实体</summary>
	public Guid AggregateId { get; set; }

	/// <summary>集成事件类型全名（如 OrderCreatedIntegrationEvent）</summary>
	public string EventType { get; set; } = string.Empty;

	/// <summary>序列化的事件负载（JSON）</summary>
	public string Payload { get; set; } = string.Empty;

	/// <summary>消息状态</summary>
	public OutboxStatus Status { get; set; } = OutboxStatus.Pending;

	/// <summary>已重试次数</summary>
	public int RetryCount { get; set; }

	/// <summary>下一次重试时间（UTC），用于指数退避</summary>
	public DateTime? NextRetryAtUtc { get; set; }

	/// <summary>事件发生时间（UTC）</summary>
	public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

	/// <summary>成功发送时间（UTC）</summary>
	public DateTime? SentAtUtc { get; set; }

	/// <summary>最后一次错误信息</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>W3C TraceId，用于关联分布式链路（创建时自动从 Activity.Current 提取）</summary>
	public string? TraceId { get; set; } = System.Diagnostics.Activity.Current?.TraceId.ToString();
}
