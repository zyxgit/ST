using ST.MS.OperationLog.Application.Dtos.OperationLog;
using ST.Shared.Application;
using ST.Shared.Application.Dtos;

namespace ST.MS.OperationLog.Application.IServices;

public interface IOperationLogQueryService : IAppService
{
	Task<PagedResultDto<OperationLogListItemDto>> GetPageAsync(OperationLogQueryInputDto input);

	Task<OperationLogDetailDto> GetDetailAsync(long id);
}
