using System.Diagnostics.Metrics;

namespace ST.MS.Inventory.Application;

/// <summary>
/// Inventory 服务自定义 OpenTelemetry 指标。
/// Meter 名称：ST.Inventory
/// </summary>
public static class InventoryMetrics
{
	public static readonly Meter Meter = new("ST.Inventory", "1.0.0");

	/// <summary>库存冻结成功数</summary>
	public static readonly Counter<long> FreezeSuccess =
		Meter.CreateCounter<long>("st.inventory.freeze.success", description: "库存冻结成功数");

	/// <summary>库存冻结失败数（含超卖）</summary>
	public static readonly Counter<long> FreezeFailed =
		Meter.CreateCounter<long>("st.inventory.freeze.failed", description: "库存冻结失败数");

	/// <summary>库存释放数</summary>
	public static readonly Counter<long> Released =
		Meter.CreateCounter<long>("st.inventory.released", description: "库存释放数");

	/// <summary>库存冻结耗时 (ms)</summary>
	public static readonly Histogram<double> FreezeDurationMs =
		Meter.CreateHistogram<double>("st.inventory.freeze.duration_ms", description: "库存冻结耗时(ms)");
}
