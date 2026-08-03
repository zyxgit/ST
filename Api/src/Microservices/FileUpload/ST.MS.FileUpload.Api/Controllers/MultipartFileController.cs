using ST.MS.FileUpload.Application.Dtos;
using ST.MS.FileUpload.Application.IServices;
using ST.Shared.Security;

namespace ST.MS.FileUpload.Api.Controllers;

/// <summary>
/// 分片上传管理
/// </summary>
[Route("api/files/multipart")]
public sealed class MultipartFileController : AbstractControllerBase
{
	private readonly IMultipartUploadService _multipartUploadService;
	private readonly IUserContext _userContext;

	public MultipartFileController(IMultipartUploadService multipartUploadService, IUserContext userContext)
	{
		_multipartUploadService = multipartUploadService;
		_userContext = userContext;
	}

	/// <summary>
	/// 初始化分片上传
	/// </summary>
	/// <param name="request">上传参数</param>
	[HttpPost("init")]
	[PermissionAuthorize(Permission.FileUpload)]
	[OperationLog("初始化分片上传", RecordRequest = true, RecordResponse = true)]
	public async Task<IActionResult> InitUpload([FromBody] InitUploadRequestDto request)
	{
		var userId = _userContext.UserId ?? throw new BusinessException("用户未登录");
		var result = await _multipartUploadService.InitUploadAsync(request, userId, _userContext.NickName);
		return CreatedAtAction(nameof(GetStatus), new { uploadId = result.UploadId }, result);
	}

	/// <summary>
	/// 查询上传状态（用于断点续传）
	/// </summary>
	/// <param name="uploadId">上传会话 ID</param>
	[HttpGet("{uploadId:guid}/status")]
	[PermissionAuthorize(Permission.FileUpload)]
	public async Task<IActionResult> GetStatus(Guid uploadId)
	{
		var result = await _multipartUploadService.GetUploadStatusAsync(uploadId);
		return Ok(result);
	}

	/// <summary>
	/// 上传单个分片
	/// </summary>
	/// <param name="uploadId">上传会话 ID</param>
	/// <param name="chunkIndex">分片序号（从 0 开始）</param>
	/// <param name="file">分片文件</param>
	/// <param name="chunkHash">分片 SHA256 Hash（可选）</param>
	[HttpPost("{uploadId:guid}/chunks/{chunkIndex:int}")]
	[PermissionAuthorize(Permission.FileUpload)]
	[RequestSizeLimit(100 * 1024 * 1024)] // 100MB 单分片上限
	[OperationLog("上传分片", RecordRequest = true, RecordResponse = false)]
	public async Task<IActionResult> UploadChunk(
		Guid uploadId,
		int chunkIndex,
		IFormFile file,
		[FromForm] string? chunkHash = null)
	{
		if (file is null || file.Length == 0)
		{
			throw new BusinessException("请选择要上传的分片文件");
		}

		await using var stream = file.OpenReadStream();
		await _multipartUploadService.UploadChunkAsync(uploadId, chunkIndex, stream, chunkHash);

		return Ok(new
		{
			uploadId,
			chunkIndex,
			size = file.Length,
			status = "uploaded"
		});
	}

	/// <summary>
	/// 完成上传（触发异步合并）
	/// </summary>
	/// <param name="uploadId">上传会话 ID</param>
	[HttpPost("{uploadId:guid}/complete")]
	[PermissionAuthorize(Permission.FileUpload)]
	[OperationLog("完成分片上传", RecordRequest = true, RecordResponse = true)]
	public async Task<IActionResult> CompleteUpload(Guid uploadId)
	{
		await _multipartUploadService.CompleteUploadAsync(uploadId);
		return Ok(new { uploadId, status = "merging" });
	}

	/// <summary>
	/// 秒传检查
	/// </summary>
	/// <param name="request">Hash 和文件大小</param>
	[HttpPost("check-by-hash")]
	[PermissionAuthorize(Permission.FileUpload)]
	public async Task<IActionResult> CheckByHash([FromBody] CheckByHashRequestDto request)
	{
		var result = await _multipartUploadService.CheckByHashAsync(request);
		return Ok(result);
	}

	/// <summary>
	/// 取消上传
	/// </summary>
	/// <param name="uploadId">上传会话 ID</param>
	[HttpDelete("{uploadId:guid}")]
	[PermissionAuthorize(Permission.FileUpload)]
	[OperationLog("取消分片上传", RecordRequest = true, RecordResponse = false)]
	public async Task<IActionResult> CancelUpload(Guid uploadId)
	{
		await _multipartUploadService.CancelUploadAsync(uploadId);
		return NoContent();
	}
}
