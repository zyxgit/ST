using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ST.MS.FileUpload.Api.Filters;
using ST.MS.FileUpload.Application.Dtos;
using ST.MS.FileUpload.Application.IServices;
using ST.MS.FileUpload.Domain;
using ST.MS.FileUpload.Domain.Entities;
using ST.MS.FileUpload.Domain.Services;
using ST.Shared.Attributes;
using ST.Shared.Exceptions;
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
    private readonly ISignedUrlService _signedUrlService;
    private readonly FileStorageOptions _options;
    private readonly IUserContext _userContext;

    public FileController(IFileAppService fileAppService, ISignedUrlService signedUrlService, IOptions<FileStorageOptions> options, IUserContext userContext)
    {
        _fileAppService = fileAppService;
        _signedUrlService = signedUrlService;
        _options = options.Value;
        _userContext = userContext;
    }

    /// <summary>
    /// 文件列表分页查询
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "perm:system:file:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> GetList([FromQuery] FileQueryInputDto input)
    {
        var result = await _fileAppService.GetListAsync(input);
        return Ok(result);
    }

    /// <summary>
    /// 上传文件
    /// </summary>
    /// <param name="file">表单文件</param>
    /// <param name="accessLevel">访问级别（Public/Private，默认 Private）</param>
    [HttpPost("upload")]
    [Authorize(Policy = "perm:system:file:upload", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
    /// 删除文件（上传者或拥有 FileDelete 权限的管理员可删除）
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "perm:system:file:delete", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [OperationLog("删除文件", RecordRequest = true, RecordResponse = false)]
    public async Task Delete(Guid id)
    {
        var userId = _userContext.UserId ?? throw new BusinessException("用户未登录");
        await _fileAppService.DeleteAsync(id, userId, true);
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = "perm:system:file:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Get(Guid id)
    {
        var result = await _fileAppService.GetAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// 下载文件（Private 文件仅上传者可下载）
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [Authorize(Policy = "perm:system:file:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [OperationLog("下载文件", RecordRequest = true, RecordResponse = false)]
    public async Task<IActionResult> Download(Guid id)
    {
        var userId = _userContext.UserId ?? throw new BusinessException("用户未登录");
        var result = await _fileAppService.DownloadWithAuthAsync(id, userId);
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

    /// <summary>
    /// 生成签名下载 URL
    /// </summary>
    /// <param name="request">请求参数</param>
    [HttpPost("signed-url")]
    [Authorize(Policy = "perm:system:file:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [OperationLog("生成签名URL", RecordRequest = true, RecordResponse = true)]
    public IActionResult GenerateSignedUrl([FromBody] GenerateSignedUrlRequestDto request)
    {
        var result = _signedUrlService.GenerateSignedUrl(request.FileId, request.ExpiresIn, _userContext.UserId);

        return Ok(new GenerateSignedUrlResultDto
        {
            Url = result.Url,
            ExpiresAtUtc = result.ExpiresAtUtc,
            ExpiresIn = result.ExpiresIn
        });
    }

    /// <summary>
    /// 通过签名 URL 下载文件（无需认证）
    /// </summary>
    /// <param name="token">签名令牌</param>
    /// <param name="sig">签名</param>
    [HttpGet("signed/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadBySignedUrl(string token, [FromQuery] string sig)
    {
        // 验证签名
        var validation = _signedUrlService.ValidateSignedUrl(token, sig);
        if (!validation.IsValid)
        {
            throw new BusinessException(validation.ErrorMessage ?? "签名 URL 无效");
        }

        // 下载文件
        var result = await _fileAppService.DownloadAsync(validation.FileId);
        return File(result.Stream, result.ContentType, result.FileName);
    }
}
