using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Services;

namespace ST.MS.FileUpload.Infra.Services;

/// <summary>
/// 本地文件存储实现
/// 文件按 yyyy/MM/dd 分目录存储，文件名使用 GUID 防重复
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _uploadRoot;

    public LocalFileStorageService(IOptions<FileStorageOptions> options)
    {
        _uploadRoot = Path.GetFullPath(options.Value.UploadRoot);
        Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var guid = Guid.CreateVersion7().ToString();
        var relativePath = Path.Combine(datePath, $"{guid}{ext}").Replace('\\', '/');

        var physicalPath = Path.Combine(_uploadRoot, datePath, $"{guid}{ext}");
        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        await using var fileStream = File.Create(physicalPath);
        await stream.CopyToAsync(fileStream);

        return relativePath;
    }

    public Task DeleteAsync(string filePath)
    {
        var physicalPath = GetSafePhysicalPath(filePath);

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }

    public string GetUrl(string filePath)
    {
        return "/" + filePath.Replace('\\', '/');
    }

    public Task<Stream> GetStreamAsync(string filePath)
    {
        var physicalPath = GetSafePhysicalPath(filePath);
        if (!File.Exists(physicalPath))
            throw new FileNotFoundException("文件不存在", physicalPath);

        return Task.FromResult<Stream>(File.OpenRead(physicalPath));
    }

    private string GetSafePhysicalPath(string filePath)
    {
        var combined = Path.Combine(_uploadRoot, filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        var fullPath = Path.GetFullPath(combined);
        var rootDir = Path.GetFullPath(_uploadRoot);

        if (!fullPath.StartsWith(rootDir, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("文件路径越权访问");

        return fullPath;
    }
}
