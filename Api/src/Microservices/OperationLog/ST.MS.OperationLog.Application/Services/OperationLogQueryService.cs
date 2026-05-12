using Microsoft.EntityFrameworkCore;
using ST.MS.OperationLog.Application.Dtos.OperationLog;
using ST.MS.OperationLog.Application.IServices;
using ST.MS.OperationLog.Infra.DbContext;
using ST.Shared.Application.Dtos;
using ST.Shared.Application.Services;
using ST.Shared.Exceptions;

namespace ST.MS.OperationLog.Application.Services;

public sealed class OperationLogQueryService : AbstractAppService, IOperationLogQueryService
{
	private readonly OperationLogDbContext _dbContext;

	public OperationLogQueryService(OperationLogDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<PagedResultDto<OperationLogListItemDto>> GetPageAsync(OperationLogQueryInputDto input)
	{
		var (pageIndex, pageSize, skip) = input.Normalize();

		var query = _dbContext.OperationLogs
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(input.ServiceName))
		{
			var serviceName = input.ServiceName.Trim();
			query = query.Where(x => x.ServiceName.Contains(serviceName));
		}

		if (input.UserId.HasValue)
		{
			query = query.Where(x => x.UserId == input.UserId.Value);
		}

		if (!string.IsNullOrWhiteSpace(input.TraceId))
		{
			var traceId = input.TraceId.Trim();
			query = query.Where(x => x.TraceId == traceId);
		}

		if (!string.IsNullOrWhiteSpace(input.Method))
		{
			var method = input.Method.Trim().ToUpperInvariant();
			query = query.Where(x => x.Method == method);
		}

		if (!string.IsNullOrWhiteSpace(input.Path))
		{
			var path = input.Path.Trim();
			query = query.Where(x => x.Path.Contains(path));
		}

		if (!string.IsNullOrWhiteSpace(input.OperationName))
		{
			var operationName = input.OperationName.Trim();
			query = query.Where(x => x.OperationName.Contains(operationName));
		}

		if (input.Success.HasValue)
		{
			query = query.Where(x => x.Success == input.Success.Value);
		}

		if (input.StatusCode.HasValue)
		{
			query = query.Where(x => x.StatusCode == input.StatusCode.Value);
		}

		if (input.StartTimeUtc.HasValue)
		{
			query = query.Where(x => x.CreatedAtUtc >= input.StartTimeUtc.Value);
		}

		if (input.EndTimeUtc.HasValue)
		{
			query = query.Where(x => x.CreatedAtUtc <= input.EndTimeUtc.Value);
		}

		if (!string.IsNullOrWhiteSpace(input.Keyword))
		{
			var keyword = input.Keyword.Trim();
			query = query.Where(x =>
				(x.UserName != null && x.UserName.Contains(keyword)) ||
				x.OperationName.Contains(keyword) ||
				x.Path.Contains(keyword) ||
				(x.ExceptionMessage != null && x.ExceptionMessage.Contains(keyword)));
		}

		var totalCount = await query.LongCountAsync();
		var items = await query
			.OrderByDescending(x => x.CreatedAtUtc)
			.Skip(skip)
			.Take(pageSize)
			.Select(x => new OperationLogListItemDto
			{
				Id = x.Id,
				CreatedAtUtc = x.CreatedAtUtc,
				ServiceName = x.ServiceName,
				UserId = x.UserId,
				UserName = x.UserName,
				OperationName = x.OperationName,
				Path = x.Path,
				Method = x.Method,
				Ip = x.Ip,
				StatusCode = x.StatusCode,
				Success = x.Success,
				DurationMs = x.DurationMs,
				TraceId = x.TraceId,
				ExceptionMessage = x.ExceptionMessage
			})
			.ToListAsync();

		return new PagedResultDto<OperationLogListItemDto>
		{
			PageIndex = pageIndex,
			PageSize = pageSize,
			TotalCount = totalCount,
			Items = items
		};
	}

	public async Task<OperationLogDetailDto> GetDetailAsync(long id)
	{
		return await _dbContext.OperationLogs
			.AsNoTracking()
			.Where(x => x.Id == id)
			.Select(x => new OperationLogDetailDto
			{
				Id = x.Id,
				CreatedAtUtc = x.CreatedAtUtc,
				ServiceName = x.ServiceName,
				TraceId = x.TraceId,
				SpanId = x.SpanId,
				UserId = x.UserId,
				UserName = x.UserName,
				OperationName = x.OperationName,
				Path = x.Path,
				Method = x.Method,
				Ip = x.Ip,
				StatusCode = x.StatusCode,
				Success = x.Success,
				DurationMs = x.DurationMs,
				RequestJson = x.RequestJson,
				ResponseJson = x.ResponseJson,
				ExceptionType = x.ExceptionType,
				ExceptionMessage = x.ExceptionMessage,
				ExceptionStackTrace = x.ExceptionStackTrace,
				TagsJson = x.TagsJson,
				ExtraJson = x.ExtraJson
			})
			.FirstOrDefaultAsync()
			?? throw new BusinessException("日志不存在");
	}
}
