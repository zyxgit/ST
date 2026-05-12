using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Api.Filters;
using ST.MS.FileUpload.Application.IServices;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Entities;
using ST.Shared.Attributes;
using ST.Shared.Security;
using ST.Shared.WebApi.Controller;

namespace ST.MS.FileUpload.Api.Controllers;

/// <summary>
/// 文件上传与管理
/// </summary>
[Route("api/files")]
public sealed class FileController : AbstractControllerBase
{
    private readonly IFileAppService _fileAppService;
    private readonly FileStorageOptions _options;
    private readonly IUserContext _userContext;

    public FileController(IFileAppService fileAppService, IOptions<FileStorageOptions> options, IUserContext userContext)
    {
        _fileAppService = fileAppService;
        _options = options.Value;
        _userContext = userContext;
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">表单文件</param>
    /// <param name="accessLevel">访问级别（Public/Private，默认 Private）</param>
    [HttpPost("upload")]
    [RequestSizeLimit(200 * 1024 * 1024)] // 200MB 硬上限，实际限制由配置控制
    [ServiceFilter<FileUploadValidationFilter>]
    [OperationLog("文件上传", RecordRequest = true, RecordResponse = true)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] FileAccessLevel? accessLevel = null)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessException("请选择要上传的文件");
        }

        await using var stream = file.OpenReadStream();
        var result = await _fileAppService.UploadAsync(stream, file.FileName, file.ContentType, accessLevel ?? _options.DefaultAccessLevel, _userContext.NickName);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    [HttpDelete("{id:guid}")]
    [OperationLog("删除文件", RecordRequest = true, RecordResponse = false)]
    public async Task Delete(Guid id)
    {
        await _fileAppService.DeleteAsync(id);
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _fileAppService.GetAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// 下载文件（不直接暴露存储路径）
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [OperationLog("下载文件", RecordRequest = true, RecordResponse = false)]
    public async Task<IActionResult> Download(Guid id)
    {
        var result = await _fileAppService.DownloadAsync(id);
        return File(result.Stream, result.ContentType, result.FileName);
    }

    /// <summary>
    /// 公开下载文件（仅 Public 文件可用，无需认证）
    /// </summary>
    [HttpGet("{id:guid}/public/download")]
    [AllowAnonymous]
    public async Task<IActionResult> PublicDownload(Guid id)
    {
        var result = await _fileAppService.DownloadPublicAsync(id);
        return File(result.Stream, result.ContentType, result.FileName);
    }
}
