using System.Diagnostics.Metrics;

namespace ST.MS.Payment.Application;

/// <summary>
/// Payment 服务自定义 OpenTelemetry 指标。
/// Meter 名称：ST.Payment
/// </summary>
public static class PaymentMetrics
{
	public static readonly Meter Meter = new("ST.Payment", "1.0.0");

	/// <summary>支付成功数</summary>
	public static readonly Counter<long> Succeeded =
		Meter.CreateCounter<long>("st.payment.succeeded", description: "支付成功数");

	/// <summary>支付失败数</summary>
	public static readonly Counter<long> Failed =
		Meter.CreateCounter<long>("st.payment.failed", description: "支付失败数");
}
