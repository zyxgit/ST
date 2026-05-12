using ST.MS.FileUpload.Domain.Entities;

namespace ST.MS.FileUpload.Domain;

/// <summary>
/// 文件存储与上传验证配置
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>存储类型，决定使用哪种文件存储实现</summary>
    public FileStorageType Type { get; set; } = FileStorageType.Local;

    /// <summary>上传根目录（本地模式）或存储桶（MinIO / OSS 模式）</summary>
    public string UploadRoot { get; set; } = "uploads";

    // ===== 文件验证规则 =====

    /// <summary>允许的 MIME 类型白名单，为空则不限制</summary>
    public string[] AllowedContentTypes { get; set; } = [];

    /// <summary>允许的文件扩展名白名单（含 . 前缀），为空则不限制</summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>最大文件大小（字节），默认 10MB</summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024;

    /// <summary>默认访问级别</summary>
    public FileAccessLevel DefaultAccessLevel { get; set; } = FileAccessLevel.Private;
}
