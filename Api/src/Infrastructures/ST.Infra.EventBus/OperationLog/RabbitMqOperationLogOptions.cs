namespace ST.Infra.EventBus.OperationLog;

public sealed class RabbitMqOperationLogOptions
{
	public string HostName { get; set; } = "localhost";

	public int Port { get; set; } = 5672;

	public string UserName { get; set; } = "guest";

	public string Password { get; set; } = "guest";

	public string VirtualHost { get; set; } = "/";

	/// <summary>
	/// 操作日志专用 Exchange（direct）。
	/// </summary>
	public string ExchangeName { get; set; } = "st.operationlog";

	/// <summary>
	/// 路由键（消费者用同一个 routingKey 绑定队列）。
	/// </summary>
	public string RoutingKey { get; set; } = "operation_log";

	/// <summary>
	/// 仅消费者使用：队列名。
	/// </summary>
	public string QueueName { get; set; } = "st.operationlog.consumer";

	/// <summary>
	/// 仅消费者使用：预取数量。
	/// </summary>
	public ushort PrefetchCount { get; set; } = 50;

	/// <summary>
	/// 仅消费者使用：消费失败是否重新入队。
	/// </summary>
	public bool RequeueOnError { get; set; } = false;

	public bool Durable { get; set; } = true;

	public bool AutoDelete { get; set; } = false;

	public int PublishRetryCount { get; set; } = 3;

	// ===== 批量消费配置 =====

	/// <summary>
	/// 是否启用批量消费模式。
	/// </summary>
	public bool EnableBatchConsumer { get; set; } = true;

	/// <summary>
	/// 批量写库大小（条数）。
	/// </summary>
	public int BatchSize { get; set; } = 50;

	/// <summary>
	/// 批量写库时间间隔（秒）。
	/// 超过此时间即使未达到 BatchSize 也会写库。
	/// </summary>
	public int FlushIntervalSeconds { get; set; } = 5;

	/// <summary>
	/// 最大重试次数。超过后消息不再重试（后续可发往死信队列）。
	/// </summary>
	public int MaxRetryCount { get; set; } = 3;

	/// <summary>
	/// 批量写库失败时是否降级为单条写入。
	/// </summary>
	public bool FallbackToSingleOnBatchFailure { get; set; } = true;
}
