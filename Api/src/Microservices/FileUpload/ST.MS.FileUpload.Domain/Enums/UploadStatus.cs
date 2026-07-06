namespace ST.MS.FileUpload.Domain.Enums;

/// <summary>
/// 分片上传状态。
/// </summary>
public enum UploadStatus
{
	/// <summary>上传中</summary>
	Uploading = 0,

	/// <summary>合并中</summary>
	Merging = 1,

	/// <summary>已完成</summary>
	Completed = 2,

	/// <summary>失败</summary>
	Failed = 3,

	/// <summary>已过期（清理未完成的上传）</summary>
	Expired = 4
}
