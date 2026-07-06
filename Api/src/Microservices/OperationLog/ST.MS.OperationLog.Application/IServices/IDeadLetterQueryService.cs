using ST.MS.OperationLog.Application.Dtos.DeadLetter;
using ST.Shared.Application;
using ST.Shared.Application.Dtos;

namespace ST.MS.OperationLog.Application.IServices;

/// <summary>
/// 死信消息查询服务接口。
/// </summary>
public interface IDeadLetterQueryService : IAppService
{
	/// <summary>
	/// 查询死信消息（分页）。
	/// </summary>
	Task<PagedResultDto<DeadLetterListItemDto>> QueryAsync(DeadLetterQueryInputDto input);

	/// <summary>
	/// 获取单条死信消息详情。
	/// </summary>
	Task<DeadLetterDetailDto> GetByIdAsync(Guid id);
}
