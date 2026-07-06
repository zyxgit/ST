namespace ST.MS.OperationLog.Infra.Entities;

/// <summary>
/// 死信消息实体。
/// 记录消费失败的操作日志消息。
/// </summary>
public sealed class DeadLetterMessage
{
	/// <summary>主键 ID</summary>
	public Guid Id { get; set; } = Guid.CreateVersion7();

	/// <summary>原始消息（JSON 序列化）</summary>
	public string OriginalMessage { get; set; } = string.Empty;

	/// <summary>队列名称</summary>
	public string QueueName { get; set; } = string.Empty;

	/// <summary>交换机名称</summary>
	public string ExchangeName { get; set; } = string.Empty;

	/// <summary>路由键</summary>
	public string RoutingKey { get; set; } = string.Empty;

	/// <summary>错误信息</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>错误堆栈</summary>
	public string? ErrorStackTrace { get; set; }

	/// <summary>已重试次数</summary>
	public int RetryCount { get; set; }

	/// <summary>最大重试次数</summary>
	public int MaxRetryCount { get; set; }

	/// <summary>消息创建时间（来自原始消息）</summary>
	public DateTime? MessageCreatedAtUtc { get; set; }

	/// <summary>进入死信队列时间</summary>
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

	/// <summary>重放时间</summary>
	public DateTime? ReplayedAtUtc { get; set; }

	/// <summary>重放结果</summary>
	public string? ReplayResult { get; set; }

	/// <summary>是否已重放（计算属性，不映射到数据库列）</summary>
	[System.ComponentModel.DataAnnotations.Schema.NotMapped]
	public bool IsReplayed => ReplayedAtUtc.HasValue;
}
