namespace ST.MS.Order.Application.Options;

/// <summary>
/// 订单超时自动取消配置。
/// </summary>
public sealed class OrderTimeoutOptions
{
	/// <summary>配置节名称</summary>
	public const string SectionName = "OrderTimeout";

	/// <summary>是否启用超时检查</summary>
	public bool Enabled { get; set; } = true;

	/// <summary>检查间隔（秒），默认 60</summary>
	public int CheckIntervalSeconds { get; set; } = 60;

	/// <summary>支付超时时间（分钟），默认 5</summary>
	public int PaymentTimeoutMinutes { get; set; } = 5;

	/// <summary>每次批量处理的最大订单数，默认 100</summary>
	public int BatchSize { get; set; } = 100;
}
