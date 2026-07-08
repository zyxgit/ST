using ST.MS.FileUpload.Application.Dtos;
using ST.MS.FileUpload.Domain.Entities;
using ST.Shared.Application;
using ST.Shared.Application.Dtos;

namespace ST.MS.FileUpload.Application.IServices;

/// <summary>
/// 文件应用服务接口
/// </summary>
public interface IFileAppService : IAppService
{
    /// <summary>上传文件</summary>
    Task<FileUploadResultDto> UploadAsync(Stream stream, string fileName, string contentType, FileAccessLevel accessLevel = FileAccessLevel.Private, string? uploaderName = null);

    /// <summary>删除文件（上传者或拥有 FileDelete 权限的管理员可删除）</summary>
    Task DeleteAsync(Guid id, Guid userId, bool hasDeletePermission = false);

    /// <summary>获取文件信息</summary>
    Task<FileInfoDto> GetAsync(Guid id);

    /// <summary>文件列表分页查询</summary>
    Task<PagedResultDto<FileInfoDto>> GetListAsync(FileQueryInputDto input);

    /// <summary>下载文件（认证用户，Private 文件仅上传者可下载）</summary>
    Task<FileDownloadResultDto> DownloadWithAuthAsync(Guid id, Guid userId);

    /// <summary>下载文件（签名 URL 专用，不做 Owner 校验）</summary>
    Task<FileDownloadResultDto> DownloadAsync(Guid id);

    /// <summary>公开下载文件（仅允许 Public 文件，无需认证）</summary>
    Task<FileDownloadResultDto> DownloadPublicAsync(Guid id);
}
