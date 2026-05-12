using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Services;

namespace ST.MS.FileUpload.Infra.Services;

/// <summary>
/// MinIO 文件存储实现（模拟）
/// 生产环境接入 MinIO .NET SDK（Minio NuGet 包）
/// </summary>
public sealed class MinIOFileStorageService : IFileStorageService
{
    private readonly string _bucket;

    public MinIOFileStorageService(IOptions<FileStorageOptions> options)
    {
        _bucket = options.Value.UploadRoot;
    }

    public Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName);
        var datePath = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var guid = Guid.CreateVersion7().ToString();
        var relativePath = Path.Combine("uploads", datePath, $"{guid}{ext}").Replace('\\', '/');

        // 模拟上传：生产环境调用 MinIO SDK PutObjectAsync
        // 如 await _minio.PutObjectAsync(new PutObjectArgs().WithBucket(_bucket).WithObject(relativePath).WithStream(stream).WithContentType(contentType));
        return Task.FromResult(relativePath);
    }

    public Task DeleteAsync(string filePath)
    {
        // 模拟删除：生产环境调用 MinIO SDK RemoveObjectAsync
        // 如 await _minio.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_bucket).WithObject(filePath));
        return Task.CompletedTask;
    }

    public string GetUrl(string filePath)
    {
        return $"/{_bucket}/{filePath.TrimStart('/')}";
    }

    public Task<Stream> GetStreamAsync(string filePath)
    {
        // 模拟下载：生产环境调用 MinIO SDK GetObjectAsync
        // 如 var obj = await _minio.GetObjectAsync(new GetObjectArgs().WithBucket(_bucket).WithObject(filePath));
        // return obj.ResponseStream;
        var memoryStream = new MemoryStream();
        return Task.FromResult<Stream>(memoryStream);
    }
}
