namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// Outbox Publisher 后台服务配置。
/// </summary>
public sealed class OutboxPublisherOptions
{
	/// <summary>
	/// 配置节点名称。
	/// </summary>
	public const string SectionName = "OutboxPublisher";

	/// <summary>
	/// 轮询间隔（秒）。默认 5 秒。
	/// </summary>
	public int PollingIntervalSeconds { get; set; } = 5;

	/// <summary>
	/// 每批拉取消息数量。默认 50。
	/// </summary>
	public int BatchSize { get; set; } = 50;

	/// <summary>
	/// 最大重试次数。超过后消息标记为 Failed 不再自动重试。默认 5。
	/// </summary>
	public int MaxRetryCount { get; set; } = 5;

	/// <summary>
	/// 重试退避基数（秒）。实际延迟 = BaseRetryDelaySeconds * 2^RetryCount。默认 10。
	/// </summary>
	public int BaseRetryDelaySeconds { get; set; } = 10;

	/// <summary>
	/// RabbitMQ Exchange 名称。默认 st.eventbus（与 EventBus 交换机一致，确保订阅方能收到 Outbox 消息）。
	/// </summary>
	public string ExchangeName { get; set; } = "st.eventbus";

	/// <summary>
	/// 是否持久化消息。默认 true。
	/// </summary>
	public bool Durable { get; set; } = true;

	/// <summary>
	/// RabbitMQ 连接字符串键名（从 ConnectionStrings 节读取）。默认 rabbitmq。
	/// </summary>
	public string ConnectionStringName { get; set; } = "rabbitmq";

	/// <summary>
	/// RabbitMQ 主机名。当连接字符串不存在时使用。默认 localhost。
	/// </summary>
	public string HostName { get; set; } = "localhost";

	/// <summary>
	/// RabbitMQ 端口。默认 5672。
	/// </summary>
	public int Port { get; set; } = 5672;

	/// <summary>
	/// RabbitMQ 用户名。默认 guest。
	/// </summary>
	public string UserName { get; set; } = "guest";

	/// <summary>
	/// RabbitMQ 密码。默认 guest。
	/// </summary>
	public string Password { get; set; } = "guest";

	/// <summary>
	/// RabbitMQ 虚拟主机。默认 /。
	/// </summary>
	public string VirtualHost { get; set; } = "/";
}
