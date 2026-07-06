using ST.MS.FileUpload.Domain.Enums;

namespace ST.MS.FileUpload.Domain.Entities;

/// <summary>
/// 分片上传会话。
/// 记录大文件分片上传的元数据和状态。
/// </summary>
public sealed class FileUploadSession
{
	/// <summary>上传会话 ID（主键）</summary>
	public Guid Id { get; set; } = Guid.CreateVersion7();

	/// <summary>原始文件名</summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>文件 SHA256 Hash（用于秒传和完整性校验）</summary>
	public string? FileHash { get; set; }

	/// <summary>文件总大小（字节）</summary>
	public long FileSize { get; set; }

	/// <summary>分片大小（字节）</summary>
	public int ChunkSize { get; set; }

	/// <summary>总分片数</summary>
	public int TotalChunks { get; set; }

	/// <summary>已上传分片数</summary>
	public int UploadedChunks { get; set; }

	/// <summary>上传状态</summary>
	public UploadStatus Status { get; set; } = UploadStatus.Uploading;

	/// <summary>上传用户 ID</summary>
	public Guid CreatedBy { get; set; }

	/// <summary>上传用户显示名</summary>
	public string? CreatorName { get; set; }

	/// <summary>文件访问级别</summary>
	public FileAccessLevel AccessLevel { get; set; } = FileAccessLevel.Private;

	/// <summary>合并后的文件 ID（关联 FileEntity）</summary>
	public Guid? FileId { get; set; }

	/// <summary>错误信息</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>创建时间（UTC）</summary>
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

	/// <summary>更新时间（UTC）</summary>
	public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

	/// <summary>过期时间（UTC，清理未完成的上传）</summary>
	public DateTime ExpiresAtUtc { get; set; }

	/// <summary>导航属性：关联的分片列表</summary>
	public ICollection<FileUploadChunk> Chunks { get; set; } = [];

	/// <summary>
	/// 计算总分片数。
	/// </summary>
	public static int CalculateTotalChunks(long fileSize, int chunkSize)
	{
		return (int)Math.Ceiling((double)fileSize / chunkSize);
	}
}
