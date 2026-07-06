using ST.MS.FileUpload.Application.Dtos;
using ST.MS.FileUpload.Domain.Entities;
using ST.Shared.Application;

namespace ST.MS.FileUpload.Application.IServices;

/// <summary>
/// 分片上传服务接口。
/// </summary>
public interface IMultipartUploadService : IAppService
{
	/// <summary>
	/// 初始化分片上传。
	/// </summary>
	Task<InitUploadResultDto> InitUploadAsync(InitUploadRequestDto request, Guid userId, string? userName);

	/// <summary>
	/// 查询上传状态（用于断点续传）。
	/// </summary>
	Task<UploadStatusDto> GetUploadStatusAsync(Guid uploadId);

	/// <summary>
	/// 上传单个分片。
	/// </summary>
	Task UploadChunkAsync(Guid uploadId, int chunkIndex, Stream stream, string? chunkHash);

	/// <summary>
	/// 完成上传（标记为 Merging，由后台服务执行合并）。
	/// </summary>
	Task CompleteUploadAsync(Guid uploadId);

	/// <summary>
	/// 秒传检查。
	/// </summary>
	Task<CheckByHashResultDto> CheckByHashAsync(CheckByHashRequestDto request);

	/// <summary>
	/// 取消上传。
	/// </summary>
	Task CancelUploadAsync(Guid uploadId);

	/// <summary>
	/// 合并分片为完整文件（由后台合并服务调用）。
	/// </summary>
	Task MergeChunksAsync(FileUploadSession session);
}
