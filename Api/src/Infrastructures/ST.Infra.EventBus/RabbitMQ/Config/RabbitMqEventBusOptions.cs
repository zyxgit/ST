namespace ST.Infra.EventBus.RabbitMQ.Config;

public sealed class RabbitMqEventBusOptions
{
	public string HostName { get; set; } = "localhost";

	public int Port { get; set; } = 5672;

	public string UserName { get; set; } = "guest";

	public string Password { get; set; } = "guest";

	public string VirtualHost { get; set; } = "/";

	public string ExchangeName { get; set; } = "st.eventbus";

	/// <summary>
	/// 每个服务建议使用唯一队列名，用于隔离消费。
	/// </summary>
	public string QueueName { get; set; } = string.Empty;

	public bool Durable { get; set; } = true;

	public bool AutoDelete { get; set; } = false;

	public ushort PrefetchCount { get; set; } = 20;

	public int PublishRetryCount { get; set; } = 3;

	public bool RequeueOnError { get; set; } = false;

	public bool AutomaticRecoveryEnabled { get; set; } = true;

	public int NetworkRecoveryIntervalSeconds { get; set; } = 10;
}

