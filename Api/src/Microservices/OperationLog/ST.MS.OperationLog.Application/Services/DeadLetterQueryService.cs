using Microsoft.EntityFrameworkCore;
using ST.MS.OperationLog.Application.Dtos.DeadLetter;
using ST.MS.OperationLog.Application.IServices;
using ST.MS.OperationLog.Infra.DbContext;
using ST.Shared.Application.Dtos;
using ST.Shared.Exceptions;

namespace ST.MS.OperationLog.Application.Services;

/// <summary>
/// 死信消息查询服务实现。
/// </summary>
public sealed class DeadLetterQueryService : IDeadLetterQueryService
{
	private readonly OperationLogDbContext _dbContext;

	public DeadLetterQueryService(OperationLogDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	/// <inheritdoc />
	public async Task<PagedResultDto<DeadLetterListItemDto>> QueryAsync(DeadLetterQueryInputDto input)
	{
		var query = _dbContext.DeadLetterMessages.AsQueryable();

		if (!string.IsNullOrWhiteSpace(input.QueueName))
			query = query.Where(x => x.QueueName == input.QueueName);

		if (input.IsReplayed.HasValue)
			query = query.Where(x => x.IsReplayed == input.IsReplayed.Value);

		if (input.StartTime.HasValue)
			query = query.Where(x => x.CreatedAtUtc >= input.StartTime.Value);

		if (input.EndTime.HasValue)
			query = query.Where(x => x.CreatedAtUtc <= input.EndTime.Value);

		var total = await query.CountAsync();

		var items = await query
			.OrderByDescending(x => x.CreatedAtUtc)
			.Skip((input.Page - 1) * input.PageSize)
			.Take(input.PageSize)
			.Select(x => new DeadLetterListItemDto
			{
				Id = x.Id,
				QueueName = x.QueueName,
				ExchangeName = x.ExchangeName,
				RoutingKey = x.RoutingKey,
				ErrorMessage = x.ErrorMessage,
				RetryCount = x.RetryCount,
				MaxRetryCount = x.MaxRetryCount,
				MessageCreatedAtUtc = x.MessageCreatedAtUtc,
				CreatedAtUtc = x.CreatedAtUtc,
				IsReplayed = x.IsReplayed,
				ReplayedAtUtc = x.ReplayedAtUtc,
				ReplayResult = x.ReplayResult
			})
			.ToListAsync();

		return new PagedResultDto<DeadLetterListItemDto>
		{
			PageIndex = input.Page,
			PageSize = input.PageSize,
			TotalCount = total,
			Items = items
		};
	}

	/// <inheritdoc />
	public async Task<DeadLetterDetailDto> GetByIdAsync(Guid id)
	{
		var entity = await _dbContext.DeadLetterMessages.FindAsync(id);
		if (entity is null)
			throw new BusinessException("死信消息不存在");

		return new DeadLetterDetailDto
		{
			Id = entity.Id,
			QueueName = entity.QueueName,
			ExchangeName = entity.ExchangeName,
			RoutingKey = entity.RoutingKey,
			ErrorMessage = entity.ErrorMessage,
			ErrorStackTrace = entity.ErrorStackTrace,
			RetryCount = entity.RetryCount,
			MaxRetryCount = entity.MaxRetryCount,
			MessageCreatedAtUtc = entity.MessageCreatedAtUtc,
			CreatedAtUtc = entity.CreatedAtUtc,
			IsReplayed = entity.IsReplayed,
			ReplayedAtUtc = entity.ReplayedAtUtc,
			ReplayResult = entity.ReplayResult,
			OriginalMessage = entity.OriginalMessage
		};
	}
}
