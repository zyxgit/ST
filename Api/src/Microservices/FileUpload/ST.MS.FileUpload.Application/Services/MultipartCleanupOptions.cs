namespace ST.MS.FileUpload.Application.Services;

/// <summary>
/// 分片清理后台服务配置。
/// </summary>
public sealed class MultipartCleanupOptions
{
	/// <summary>配置节名称</summary>
	public const string SectionName = "MultipartCleanup";

	/// <summary>是否启用后台清理服务</summary>
	public bool Enabled { get; set; } = true;

	/// <summary>轮询间隔（秒）</summary>
	public int PollingIntervalSeconds { get; set; } = 60;

	/// <summary>每批处理的会话数量</summary>
	public int BatchSize { get; set; } = 20;

	/// <summary>Failed 状态会话的保留时间（秒），超过此时间才清理，默认 10 分钟</summary>
	public int FailedRetentionSeconds { get; set; } = 600;

	/// <summary>Completed 状态会话的保留时间（秒），超过此时间才清理文件，默认 10 分钟</summary>
	public int CompletedRetentionSeconds { get; set; } = 600;

	/// <summary>是否删除已完成会话的合并文件，默认 true</summary>
	public bool DeleteCompletedFiles { get; set; } = true;
}
