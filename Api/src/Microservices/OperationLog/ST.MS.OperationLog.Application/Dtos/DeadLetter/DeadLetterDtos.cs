namespace ST.MS.OperationLog.Application.Dtos.DeadLetter;

/// <summary>
/// 死信消息查询输入 DTO。
/// </summary>
public sealed class DeadLetterQueryInputDto
{
	/// <summary>队列名称筛选</summary>
	public string? QueueName { get; set; }

	/// <summary>是否已重放筛选</summary>
	public bool? IsReplayed { get; set; }

	/// <summary>开始时间</summary>
	public DateTime? StartTime { get; set; }

	/// <summary>结束时间</summary>
	public DateTime? EndTime { get; set; }

	/// <summary>页码（默认 1）</summary>
	public int Page { get; set; } = 1;

	/// <summary>每页条数（默认 20）</summary>
	public int PageSize { get; set; } = 20;
}

/// <summary>
/// 死信消息列表项 DTO。
/// </summary>
public sealed class DeadLetterListItemDto
{
	/// <summary>主键 ID</summary>
	public Guid Id { get; set; }

	/// <summary>队列名称</summary>
	public string QueueName { get; set; } = string.Empty;

	/// <summary>交换机名称</summary>
	public string ExchangeName { get; set; } = string.Empty;

	/// <summary>路由键</summary>
	public string RoutingKey { get; set; } = string.Empty;

	/// <summary>错误信息</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>已重试次数</summary>
	public int RetryCount { get; set; }

	/// <summary>最大重试次数</summary>
	public int MaxRetryCount { get; set; }

	/// <summary>消息创建时间</summary>
	public DateTime? MessageCreatedAtUtc { get; set; }

	/// <summary>进入死信时间</summary>
	public DateTime CreatedAtUtc { get; set; }

	/// <summary>是否已重放</summary>
	public bool IsReplayed { get; set; }

	/// <summary>重放时间</summary>
	public DateTime? ReplayedAtUtc { get; set; }

	/// <summary>重放结果</summary>
	public string? ReplayResult { get; set; }
}

/// <summary>
/// 死信消息详情 DTO。
/// </summary>
public sealed class DeadLetterDetailDto
{
	/// <summary>主键 ID</summary>
	public Guid Id { get; set; }

	/// <summary>队列名称</summary>
	public string QueueName { get; set; } = string.Empty;

	/// <summary>交换机名称</summary>
	public string ExchangeName { get; set; } = string.Empty;

	/// <summary>路由键</summary>
	public string RoutingKey { get; set; } = string.Empty;

	/// <summary>原始消息（JSON）</summary>
	public string OriginalMessage { get; set; } = string.Empty;

	/// <summary>错误信息</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>错误堆栈</summary>
	public string? ErrorStackTrace { get; set; }

	/// <summary>已重试次数</summary>
	public int RetryCount { get; set; }

	/// <summary>最大重试次数</summary>
	public int MaxRetryCount { get; set; }

	/// <summary>消息创建时间</summary>
	public DateTime? MessageCreatedAtUtc { get; set; }

	/// <summary>进入死信时间</summary>
	public DateTime CreatedAtUtc { get; set; }

	/// <summary>是否已重放</summary>
	public bool IsReplayed { get; set; }

	/// <summary>重放时间</summary>
	public DateTime? ReplayedAtUtc { get; set; }

	/// <summary>重放结果</summary>
	public string? ReplayResult { get; set; }
}

/// <summary>
/// 批量重放请求 DTO。
/// </summary>
public sealed class BatchReplayRequestDto
{
	/// <summary>要重放的死信消息 ID 列表</summary>
	public List<Guid> Ids { get; set; } = [];
}

/// <summary>
/// 批量重放结果 DTO。
/// </summary>
public sealed class BatchReplayResultDto
{
	/// <summary>重放成功数</summary>
	public int Replayed { get; set; }

	/// <summary>重放失败数</summary>
	public int Failed { get; set; }
}
