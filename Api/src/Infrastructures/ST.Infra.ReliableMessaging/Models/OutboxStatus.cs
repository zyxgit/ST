namespace ST.Infra.ReliableMessaging.Models;

/// <summary>
/// Outbox 消息状态。
/// </summary>
public enum OutboxStatus
{
	/// <summary>待发送</summary>
	Pending = 0,

	/// <summary>已成功发送至消息代理</summary>
	Sent = 1,

	/// <summary>发送失败，等待重试或人工干预</summary>
	Failed = 2
}
