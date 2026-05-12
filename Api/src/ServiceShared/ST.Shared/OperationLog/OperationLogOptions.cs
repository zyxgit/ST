namespace ST.Shared.OperationLog;

public sealed class OperationLogOptions
{
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// 启用的 Sink 名称列表（如：rabbitmq / ef / null）。
	/// 为空表示：启用所有已注册的 sink。
	/// </summary>
	public string[] Sinks { get; set; } = [];

	/// <summary>
	/// 全局默认是否记录响应；可被 Attribute 覆盖。
	/// </summary>
	public bool RecordResponseByDefault { get; set; } = false;

	public int MaxBodyLength { get; set; } = 8_192;

	/// <summary>
	/// 采样率：1.0=全量，0.1=10%。
	/// </summary>
	public double SampleRate { get; set; } = 1.0;

	public bool MaskEnabled { get; set; } = true;

	public string Mask { get; set; } = "****";

	/// <summary>
	/// 默认敏感字段 key（大小写不敏感）。
	/// </summary>
	public string[] SensitiveKeys { get; set; } =
	[
		"password",
		"pwd",
		"token",
		"accessToken",
		"refreshToken",
		"secret",
		"authorization"
	];

	/// <summary>
	/// Channel 容量（异步落库缓冲）；满时可选择等待或丢弃。
	/// </summary>
	public int ChannelCapacity { get; set; } = 10_000;

	public bool DropWhenFull { get; set; } = true;

	/// <summary>
	/// 批量落库参数。
	/// </summary>
	public int BatchSize { get; set; } = 50;

	public int FlushIntervalMs { get; set; } = 500;
}
