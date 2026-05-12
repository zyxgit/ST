using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Application.Dtos;
using ST.MS.FileUpload.Application.IServices;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Entities;
using ST.MS.FileUpload.Domain.Services;
using ST.MS.FileUpload.Infra.DbContext;
using ST.Shared.Application.Services;
using ST.Shared.Exceptions;

namespace ST.MS.FileUpload.Application.Services;

public sealed class FileAppService : AbstractAppService, IFileAppService
{
    private readonly FileUploadDbContext _dbContext;
    private readonly IFileStorageService _storageService;
    private readonly FileStorageOptions _options;

    public FileAppService(
        FileUploadDbContext dbContext,
        IFileStorageService storageService,
        IOptions<FileStorageOptions> options)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _options = options.Value;
    }

    public async Task<FileUploadResultDto> UploadAsync(Stream stream, string fileName, string contentType, FileAccessLevel accessLevel = FileAccessLevel.Private, string? uploaderName = null)
    {
        // 1. 保存文件到存储
        var filePath = await _storageService.UploadAsync(stream, fileName, contentType);

        // 2. 创建数据库记录
        var extension = Path.GetExtension(fileName);
        var entity = new FileEntity(fileName, filePath, stream.Length, contentType, extension, accessLevel, uploaderName);

        _dbContext.Files.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 3. 返回下载 URL（不暴露存储路径）
        var downloadUrl = accessLevel == FileAccessLevel.Public
            ? $"/api/files/{entity.Id}/public/download"
            : $"/api/files/{entity.Id}/download";

        return new FileUploadResultDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            FileSize = entity.FileSize,
            ContentType = entity.ContentType,
            Url = downloadUrl,
            UploaderName = entity.UploaderName
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await LoadFileAsync(id);

        await _storageService.DeleteAsync(entity.FilePath);

        _dbContext.Files.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<FileInfoDto> GetAsync(Guid id)
    {
        var entity = await LoadFileAsync(id);

        var downloadUrl = entity.AccessLevel == FileAccessLevel.Public
            ? $"/api/files/{entity.Id}/public/download"
            : $"/api/files/{entity.Id}/download";

        return new FileInfoDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            FilePath = entity.FilePath,
            FileSize = entity.FileSize,
            ContentType = entity.ContentType,
            Extension = entity.Extension,
            Url = downloadUrl,
            CreateTime = entity.CreateTime,
            UploaderName = entity.UploaderName
        };
    }

    public async Task<FileDownloadResultDto> DownloadAsync(Guid id)
    {
        var entity = await _dbContext.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BusinessException("文件不存在");

        var stream = await _storageService.GetStreamAsync(entity.FilePath);

        return new FileDownloadResultDto
        {
            Stream = stream,
            FileName = entity.FileName,
            ContentType = entity.ContentType
        };
    }

    public async Task<FileDownloadResultDto> DownloadPublicAsync(Guid id)
    {
        var entity = await _dbContext.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BusinessException("文件不存在");

        if (entity.AccessLevel != FileAccessLevel.Public)
            throw new BusinessException("文件非公开，无法通过公开链接下载");

        var stream = await _storageService.GetStreamAsync(entity.FilePath);

        return new FileDownloadResultDto
        {
            Stream = stream,
            FileName = entity.FileName,
            ContentType = entity.ContentType
        };
    }

    private async Task<FileEntity> LoadFileAsync(Guid id)
    {
        return await _dbContext.Files
                   .AsNoTracking()
                   .FirstOrDefaultAsync(x => x.Id == id)
               ?? throw new BusinessException("文件不存在");
    }
}
