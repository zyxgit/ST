namespace ST.MS.FileUpload.Application.Dtos;

/// <summary>
/// 初始化分片上传请求。
/// </summary>
public sealed class InitUploadRequestDto
{
	/// <summary>原始文件名</summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>文件总大小（字节）</summary>
	public long FileSize { get; set; }

	/// <summary>分片大小（字节，默认 5MB）</summary>
	public int ChunkSize { get; set; } = 5 * 1024 * 1024;

	/// <summary>文件 SHA256 Hash（可选，用于秒传）</summary>
	public string? FileHash { get; set; }

	/// <summary>文件 MIME 类型（可选，用于校验白名单）</summary>
	public string? ContentType { get; set; }

	/// <summary>访问级别（0=Public, 1=Private，默认 Private）</summary>
	public int AccessLevel { get; set; } = 1;
}

/// <summary>
/// 初始化分片上传结果。
/// </summary>
public sealed class InitUploadResultDto
{
	/// <summary>上传会话 ID</summary>
	public Guid UploadId { get; set; }

	/// <summary>文件名</summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>文件大小</summary>
	public long FileSize { get; set; }

	/// <summary>分片大小</summary>
	public int ChunkSize { get; set; }

	/// <summary>总分片数</summary>
	public int TotalChunks { get; set; }

	/// <summary>上传状态</summary>
	public string Status { get; set; } = string.Empty;

	/// <summary>过期时间</summary>
	public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>
/// 上传状态查询结果。
/// </summary>
public sealed class UploadStatusDto
{
	/// <summary>上传会话 ID</summary>
	public Guid UploadId { get; set; }

	/// <summary>文件名</summary>
	public string FileName { get; set; } = string.Empty;

	/// <summary>文件大小</summary>
	public long FileSize { get; set; }

	/// <summary>总分片数</summary>
	public int TotalChunks { get; set; }

	/// <summary>已上传分片数</summary>
	public int UploadedChunks { get; set; }

	/// <summary>已上传的分片序号列表</summary>
	public List<int> UploadedChunkIndexes { get; set; } = [];

	/// <summary>缺失的分片序号列表</summary>
	public List<int> MissingChunkIndexes { get; set; } = [];

	/// <summary>上传状态</summary>
	public string Status { get; set; } = string.Empty;

	/// <summary>进度百分比</summary>
	public double Progress => TotalChunks > 0 ? (double)UploadedChunks / TotalChunks * 100 : 0;

	/// <summary>合并后的文件 ID（仅 Completed 状态）</summary>
	public Guid? FileId { get; set; }
}

/// <summary>
/// 秒传检查请求。
/// </summary>
public sealed class CheckByHashRequestDto
{
	/// <summary>文件 SHA256 Hash</summary>
	public string FileHash { get; set; } = string.Empty;

	/// <summary>文件大小（字节）</summary>
	public long FileSize { get; set; }
}

/// <summary>
/// 秒传检查结果。
/// </summary>
public sealed class CheckByHashResultDto
{
	/// <summary>文件是否已存在</summary>
	public bool Exists { get; set; }

	/// <summary>已存在的文件 ID（仅 Exists=true）</summary>
	public Guid? FileId { get; set; }

	/// <summary>已存在的文件名（仅 Exists=true）</summary>
	public string? FileName { get; set; }

	/// <summary>已存在的文件大小（仅 Exists=true）</summary>
	public long FileSize { get; set; }
}
