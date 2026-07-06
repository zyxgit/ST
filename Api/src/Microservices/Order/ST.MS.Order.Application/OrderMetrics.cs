using System.Diagnostics.Metrics;

namespace ST.MS.Order.Application;

/// <summary>
/// Order 服务自定义 OpenTelemetry 指标。
/// Meter 名称：ST.Order
/// </summary>
public static class OrderMetrics
{
	public static readonly Meter Meter = new("ST.Order", "1.0.0");

	/// <summary>下单成功数</summary>
	public static readonly Counter<long> OrderCreated =
		Meter.CreateCounter<long>("st.order.created", description: "下单成功数");

	/// <summary>订单取消数</summary>
	public static readonly Counter<long> OrderCanceled =
		Meter.CreateCounter<long>("st.order.canceled", description: "订单取消数");

	/// <summary>Saga 补偿次数</summary>
	public static readonly Counter<long> SagaCompensated =
		Meter.CreateCounter<long>("st.order.saga.compensated", description: "Saga 补偿次数");

	/// <summary>下单耗时 (ms)</summary>
	public static readonly Histogram<double> CreateDurationMs =
		Meter.CreateHistogram<double>("st.order.create.duration_ms", description: "下单耗时(ms)");
}
