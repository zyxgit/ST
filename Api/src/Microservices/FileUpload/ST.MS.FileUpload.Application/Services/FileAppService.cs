using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Application.Dtos;
using ST.MS.FileUpload.Application.IServices;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Entities;
using ST.MS.FileUpload.Domain.Services;
using ST.MS.FileUpload.Infra.DbContext;
using ST.Shared.Application;
using ST.Shared.Application.Dtos;
using ST.Shared.Application.Services;
using ST.Shared.Exceptions;
using ST.Shared.Security;

namespace ST.MS.FileUpload.Application.Services;

public sealed class FileAppService : AbstractAppService, IFileAppService
{
    private readonly FileUploadDbContext _dbContext;
    private readonly IFileStorageService _storageService;
    private readonly FileStorageOptions _options;
    private readonly ICurrentTenantAccessor _tenantAccessor;
    private readonly ITenantQuotaService? _quotaService;

    public FileAppService(
        FileUploadDbContext dbContext,
        IFileStorageService storageService,
        IOptions<FileStorageOptions> options,
        ICurrentTenantAccessor tenantAccessor,
        ITenantQuotaService? quotaService = null)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _options = options.Value;
        _tenantAccessor = tenantAccessor;
        _quotaService = quotaService;
    }

    public async Task<FileUploadResultDto> UploadAsync(Stream stream, string fileName, string contentType, FileAccessLevel accessLevel = FileAccessLevel.Private, string? uploaderName = null)
    {
        // 租户配额检查：单文件大小
        var fileLength = stream.Length;
        if (_tenantAccessor.TenantId.HasValue && _quotaService is not null)
        {
            await _quotaService.CheckFileSizeQuotaAsync(_tenantAccessor.TenantId.Value, fileLength);
        }
        string filePath;

        using var sha256 = SHA256.Create();
        using (var cryptoStream = new CryptoStream(stream, sha256, CryptoStreamMode.Read))
        {
            filePath = await _storageService.UploadAsync(cryptoStream, fileName, contentType);
        }
        // cryptoStream Dispose 后 sha256.Hash 才可用
        var fileHash = Convert.ToHexString(sha256.Hash!);

        // 2. 创建数据库记录
        var extension = Path.GetExtension(fileName);
        var entity = new FileEntity(fileName, filePath, fileLength, contentType, extension, accessLevel, uploaderName, fileHash);

        _dbContext.Files.Add(entity);
        await _dbContext.SaveChangesAsync();

        // 3. 返回下载 URL（不暴露存储路径）
        var downloadUrl = accessLevel == FileAccessLevel.Public
            ? $"/api/files/{entity.Id}/public/download"
            : $"/api/files/{entity.Id}/download";

        FileUploadMetrics.UploadCount.Add(1);
        FileUploadMetrics.FileSizeBytes.Record(fileLength);

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

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await LoadFileAsync(id);

        if (entity.CreateBy != userId)
            throw new BusinessException("无权删除此文件");

        await _storageService.DeleteAsync(entity.FilePath);

        _dbContext.Files.Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<FileInfoDto> GetAsync(Guid id)
    {
        var entity = await LoadFileAsync(id);
        return MapToDto(entity);
    }

    public async Task<PagedResultDto<FileInfoDto>> GetListAsync(FileQueryInputDto input)
    {
        var (pageIndex, pageSize, skip) = input.Normalize();

        var query = _dbContext.Files.AsNoTracking().AsQueryable();

        // 按文件名模糊搜索
        if (!string.IsNullOrWhiteSpace(input.Keyword))
            query = query.Where(f => f.FileName.Contains(input.Keyword));

        // 按访问级别筛选
        if (input.AccessLevel.HasValue)
            query = query.Where(f => f.AccessLevel == (FileAccessLevel)input.AccessLevel.Value);

        // 按 MIME 类型前缀筛选
        if (!string.IsNullOrWhiteSpace(input.ContentType))
            query = query.Where(f => f.ContentType.StartsWith(input.ContentType));

        var totalCount = await query.LongCountAsync();

        var items = await query
            .OrderByDescending(f => f.CreateTime)
            .Skip(skip)
            .Take(pageSize)
            .Select(f => new FileInfoDto
            {
                Id = f.Id,
                FileName = f.FileName,
                FilePath = f.FilePath,
                FileSize = f.FileSize,
                ContentType = f.ContentType,
                Extension = f.Extension,
                Url = f.AccessLevel == FileAccessLevel.Public
                    ? $"/api/files/{f.Id}/public/download"
                    : $"/api/files/{f.Id}/download",
                CreateTime = f.CreateTime,
                UploaderName = f.UploaderName,
                AccessLevel = (int)f.AccessLevel
            })
            .ToListAsync();

        return new PagedResultDto<FileInfoDto>
        {
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public async Task<FileDownloadResultDto> DownloadWithAuthAsync(Guid id, Guid userId)
    {
        var entity = await _dbContext.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new BusinessException("文件不存在");

        // Private 文件仅上传者可下载
        if (entity.AccessLevel == FileAccessLevel.Private && entity.CreateBy != userId)
            throw new BusinessException("无权下载此文件");

        var stream = await _storageService.GetStreamAsync(entity.FilePath);

        return new FileDownloadResultDto
        {
            Stream = stream,
            FileName = entity.FileName,
            ContentType = entity.ContentType
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

    private static FileInfoDto MapToDto(FileEntity entity)
    {
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
            UploaderName = entity.UploaderName,
            AccessLevel = (int)entity.AccessLevel
        };
    }
}
