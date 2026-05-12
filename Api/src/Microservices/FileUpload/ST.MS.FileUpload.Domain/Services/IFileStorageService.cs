namespace ST.MS.FileUpload.Domain.Services;

/// <summary>
/// 文件存储抽象（端口）
/// 当前实现为本地存储，可扩展为 MinIO / OSS
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="fileName">原始文件名</param>
    /// <param name="contentType">MIME 类型</param>
    /// <returns>存储路径（相对路径）</returns>
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);

    /// <summary>
    /// 删除文件
    /// </summary>
    /// <param name="filePath">存储路径</param>
    Task DeleteAsync(string filePath);

    /// <summary>
    /// 获取文件流（用于下载）
    /// </summary>
    /// <param name="filePath">存储路径</param>
    /// <returns>文件流，调用方负责释放</returns>
    Task<Stream> GetStreamAsync(string filePath);

    /// <summary>
    /// 获取文件访问 URL
    /// </summary>
    /// <param name="filePath">存储路径</param>
    /// <returns>可访问的 URL</returns>
    string GetUrl(string filePath);
}
