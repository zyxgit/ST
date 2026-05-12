namespace ST.MS.FileUpload.Domain;

/// <summary>
/// 文件存储类型
/// </summary>
public enum FileStorageType
{
    /// <summary>本地文件存储</summary>
    Local = 0,

    /// <summary>MinIO 对象存储</summary>
    MinIO = 1,

    /// <summary>阿里云 OSS</summary>
    OSS = 2
}
