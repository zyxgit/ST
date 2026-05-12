namespace ST.MS.FileUpload.Domain.Entities;

/// <summary>
/// 文件上传记录
/// </summary>
public sealed class FileEntity : DomainEntity
{
    /// <summary>原始文件名</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>存储路径（相对路径，如 uploads/2026/05/07/guid.ext）</summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>文件大小（字节）</summary>
    public long FileSize { get; private set; }

    /// <summary>MIME 类型</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>文件扩展名</summary>
    public string Extension { get; private set; } = string.Empty;

    /// <summary>访问级别</summary>
    public FileAccessLevel AccessLevel { get; private set; }

    /// <summary>上传用户显示名（冗余存储，避免联查）</summary>
    public string? UploaderName { get; private set; }

    private FileEntity() { } // EF Core

    public FileEntity(string fileName, string filePath, long fileSize, string contentType, string extension, FileAccessLevel accessLevel = FileAccessLevel.Private, string? uploaderName = null)
    {
        Id = Guid.CreateVersion7();
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
        ContentType = contentType;
        Extension = extension;
        AccessLevel = accessLevel;
        UploaderName = uploaderName;
    }
}
