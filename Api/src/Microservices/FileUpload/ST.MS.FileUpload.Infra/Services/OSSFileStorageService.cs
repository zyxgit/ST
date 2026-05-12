using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Services;

namespace ST.MS.FileUpload.Infra.Services;

/// <summary>
/// 阿里云 OSS 文件存储实现（模拟）
/// 生产环境接入 Aliyun.OSS.SDK.NetCore NuGet 包
/// </summary>
public sealed class OSSFileStorageService : IFileStorageService
{
    private readonly string _bucket;

    public OSSFileStorageService(IOptions<FileStorageOptions> options)
    {
        _bucket = options.Value.UploadRoot;
    }

    public Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var guid = Guid.CreateVersion7().ToString();
        var relativePath = Path.Combine("uploads", datePath, $"{guid}{ext}").Replace('\\', '/');

        // 模拟上传：生产环境调用 OSS SDK PutObjectAsync
        // 如 await _oss.PutObjectAsync(_bucket, relativePath, stream);
        return Task.FromResult(relativePath);
    }

    public Task DeleteAsync(string filePath)
    {
        // 模拟删除：生产环境调用 OSS SDK DeleteObjectAsync
        // 如 await _oss.DeleteObjectAsync(_bucket, filePath);
        return Task.CompletedTask;
    }

    public string GetUrl(string filePath)
    {
        return $"https://{_bucket}.oss-cn-hangzhou.aliyuncs.com/{filePath.TrimStart('/')}";
    }

    public Task<Stream> GetStreamAsync(string filePath)
    {
        // 模拟下载：生产环境调用 OSS SDK GetObjectAsync
        // 如 var obj = await _oss.GetObjectAsync(_bucket, filePath);
        // return obj.Content;
        var memoryStream = new MemoryStream();
        return Task.FromResult<Stream>(memoryStream);
    }
}
