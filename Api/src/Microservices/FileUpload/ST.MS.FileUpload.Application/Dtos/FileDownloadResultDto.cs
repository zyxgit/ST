namespace ST.MS.FileUpload.Application.Dtos;

/// <summary>
/// 文件下载结果
/// </summary>
public sealed class FileDownloadResultDto
{
    /// <summary>文件流</summary>
    public required Stream Stream { get; init; }

    /// <summary>原始文件名</summary>
    public required string FileName { get; init; }

    /// <summary>MIME 类型</summary>
    public required string ContentType { get; init; }
}
