namespace ST.MS.FileUpload.Application.Services;

/// <summary>
/// 分片合并后台服务配置。
/// </summary>
public sealed class MultipartMergeOptions
{
	/// <summary>配置节名称</summary>
	public const string SectionName = "MultipartMerge";

	/// <summary>是否启用后台合并服务</summary>
	public bool Enabled { get; set; } = true;

	/// <summary>轮询间隔（秒）</summary>
	public int PollingIntervalSeconds { get; set; } = 10;

	/// <summary>每批处理的会话数量</summary>
	public int BatchSize { get; set; } = 5;

	/// <summary>单个会话最大重试次数</summary>
	public int MaxRetryCount { get; set; } = 3;
}
