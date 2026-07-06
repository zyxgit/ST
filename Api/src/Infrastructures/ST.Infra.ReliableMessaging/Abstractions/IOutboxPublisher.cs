namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// Outbox 消息投递接口，负责将 Outbox 消息发布到消息代理。
/// </summary>
public interface IOutboxPublisher
{
	/// <summary>
	/// 发布一条 Outbox 消息到消息代理。
	/// </summary>
	/// <param name="message">待发布的 Outbox 消息</param>
	/// <param name="ct">取消令牌</param>
	Task PublishAsync(OutboxMessage message, CancellationToken ct = default);
}
