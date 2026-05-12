namespace ST.MS.FileUpload.Application.Dtos;

/// <summary>
/// 文件信息
/// </summary>
public sealed class FileInfoDto
{
    /// <summary>文件记录 ID</summary>
    public required Guid Id { get; init; }

    /// <summary>原始文件名</summary>
    public required string FileName { get; init; }

    /// <summary>存储路径</summary>
    public required string FilePath { get; init; }

    /// <summary>文件大小（字节）</summary>
    public required long FileSize { get; init; }

    /// <summary>MIME 类型</summary>
    public required string ContentType { get; init; }

    /// <summary>文件扩展名</summary>
    public required string Extension { get; init; }

    /// <summary>访问 URL</summary>
    public required string Url { get; init; }

    /// <summary>上传时间</summary>
    public required DateTime CreateTime { get; init; }

    /// <summary>上传用户</summary>
    public string? UploaderName { get; init; }
}
