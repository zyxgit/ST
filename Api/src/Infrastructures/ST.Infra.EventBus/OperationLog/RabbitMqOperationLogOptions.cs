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
}
