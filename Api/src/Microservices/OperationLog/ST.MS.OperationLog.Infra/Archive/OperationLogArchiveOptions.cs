namespace ST.MS.OperationLog.Infra.Archive;

/// <summary>
/// 操作日志归档配置。
/// </summary>
public sealed class OperationLogArchiveOptions
{
	/// <summary>配置节名称</summary>
	public const string SectionName = "OperationLog:Archive";

	/// <summary>是否启用归档</summary>
	public bool Enabled { get; set; } = false;

	/// <summary>归档天数（超过此天数的日志将被归档）</summary>
	public int ArchiveAfterDays { get; set; } = 30;

	/// <summary>每批归档数量</summary>
	public int BatchSize { get; set; } = 1000;

	/// <summary>归档存储类型</summary>
	public ArchiveStorageType StorageType { get; set; } = ArchiveStorageType.Local;

	/// <summary>本地归档路径</summary>
	public string LocalArchivePath { get; set; } = "archives/operationlog";

	/// <summary>归档文件前缀</summary>
	public string FilePrefix { get; set; } = "operationlog";

	/// <summary>是否在归档后删除源数据</summary>
	public bool DeleteAfterArchive { get; set; } = true;

	/// <summary>归档任务执行间隔（小时）</summary>
	public int ExecutionIntervalHours { get; set; } = 24;
}

/// <summary>
/// 归档存储类型。
/// </summary>
public enum ArchiveStorageType
{
	/// <summary>本地文件系统</summary>
	Local = 0,

	/// <summary>MinIO 对象存储</summary>
	MinIO = 1,

	/// <summary>阿里云 OSS</summary>
	OSS = 2
}
