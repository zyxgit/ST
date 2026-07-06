namespace ST.MS.FileUpload.Domain.Entities;

/// <summary>
/// 文件上传分片。
/// 记录每个分片的元数据和存储位置。
/// </summary>
public sealed class FileUploadChunk
{
	/// <summary>分片 ID（主键）</summary>
	public Guid Id { get; set; } = Guid.CreateVersion7();

	/// <summary>关联的上传会话 ID</summary>
	public Guid UploadId { get; set; }

	/// <summary>分片序号（从 0 开始）</summary>
	public int ChunkIndex { get; set; }

	/// <summary>分片 SHA256 Hash（校验完整性）</summary>
	public string? ChunkHash { get; set; }

	/// <summary>分片大小（字节）</summary>
	public long Size { get; set; }

	/// <summary>分片存储路径</summary>
	public string StoragePath { get; set; } = string.Empty;

	/// <summary>创建时间（UTC）</summary>
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

	/// <summary>导航属性：关联的上传会话</summary>
	public FileUploadSession Session { get; set; } = null!;
}
