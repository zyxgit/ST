using System.Diagnostics.Metrics;

namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// Outbox 基础设施自定义 OpenTelemetry 指标。
/// Meter 名称：ST.Outbox
/// </summary>
public static class OutboxMetrics
{
	public static readonly Meter Meter = new("ST.Outbox", "1.0.0");

	/// <summary>Outbox 发布成功数</summary>
	public static readonly Counter<long> Published =
		Meter.CreateCounter<long>("st.outbox.published", description: "Outbox 发布成功数");

	/// <summary>Outbox 发布失败数</summary>
	public static readonly Counter<long> Failed =
		Meter.CreateCounter<long>("st.outbox.failed", description: "Outbox 发布失败数");

	/// <summary>Outbox 重试数</summary>
	public static readonly Counter<long> Retried =
		Meter.CreateCounter<long>("st.outbox.retried", description: "Outbox 重试数");

	/// <summary>进入 Pending 队列数</summary>
	public static readonly Counter<long> Pending =
		Meter.CreateCounter<long>("st.outbox.pending", description: "进入 Pending 队列数");

	/// <summary>发布耗时 (ms)</summary>
	public static readonly Histogram<double> PublishDurationMs =
		Meter.CreateHistogram<double>("st.outbox.publish.duration_ms", description: "Outbox 发布耗时(ms)");
}
