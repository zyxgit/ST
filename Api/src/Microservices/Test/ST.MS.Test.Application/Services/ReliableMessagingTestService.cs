using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Test.Infra.DbContext;
using ST.Shared.Dependency;
using ST.Shared.Exceptions;

namespace ST.MS.Test.Application.Services;

/// <summary>
/// 可靠消息基础设施验证服务。
/// </summary>
public class ReliableMessagingTestService : ITransientDependency
{
	private readonly AppDbContext _dbContext;

	public ReliableMessagingTestService(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	/// <summary>
	/// 写入一条 Outbox 消息
	/// </summary>
	public async Task<OutboxMessage> AddOutboxMessageAsync(
		Guid aggregateId,
		string eventType,
		object payload,
		CancellationToken ct = default)
	{
		var message = new OutboxMessage
		{
			AggregateId = aggregateId,
			EventType = eventType,
			Payload = JsonSerializer.Serialize(payload),
			Status = OutboxStatus.Pending,
			OccurredAtUtc = DateTime.UtcNow
		};

		_dbContext.OutboxMessages.Add(message);
		await _dbContext.SaveChangesAsync(ct);
		return message;
	}

	/// <summary>
	/// 查询所有 Outbox 消息
	/// </summary>
	public async Task<List<OutboxMessage>> GetAllOutboxMessagesAsync(CancellationToken ct = default)
	{
		return await _dbContext.OutboxMessages
			.OrderByDescending(m => m.OccurredAtUtc)
			.ToListAsync(ct);
	}

	/// <summary>
	/// 查询待发送的 Outbox 消息
	/// </summary>
	public async Task<List<OutboxMessage>> GetPendingOutboxMessagesAsync(CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await _dbContext.OutboxMessages
			.Where(m => m.Status == OutboxStatus.Pending
				&& (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
			.OrderBy(m => m.OccurredAtUtc)
			.ToListAsync(ct);
	}

	/// <summary>
	/// 标记 Outbox 消息为已发送
	/// </summary>
	public async Task<bool> MarkOutboxAsSentAsync(Guid messageId, CancellationToken ct = default)
	{
		var message = await _dbContext.OutboxMessages.FindAsync([messageId], ct);
		if (message is null) return false;

		message.Status = OutboxStatus.Sent;
		message.SentAtUtc = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync(ct);
		return true;
	}

	/// <summary>
	/// 标记 Outbox 消息为失败
	/// </summary>
	public async Task<bool> MarkOutboxAsFailedAsync(Guid messageId, string error, CancellationToken ct = default)
	{
		var message = await _dbContext.OutboxMessages.FindAsync([messageId], ct);
		if (message is null) return false;

		message.Status = OutboxStatus.Failed;
		message.ErrorMessage = error;
		message.RetryCount++;
		message.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(5);
		await _dbContext.SaveChangesAsync(ct);
		return true;
	}

	/// <summary>
	/// 写入一条 Inbox 消息（幂等）
	/// </summary>
	public async Task<InboxMessage> AddInboxMessageAsync(
		Guid messageId,
		string consumer,
		string eventType,
		CancellationToken ct = default)
	{
		// 幂等检查
		var exists = await _dbContext.InboxMessages
			.AnyAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
		if (exists)
			throw new BusinessException($"消息 {messageId} 已被消费者 {consumer} 处理过");

		var message = new InboxMessage
		{
			MessageId = messageId,
			Consumer = consumer,
			EventType = eventType,
			ReceivedAtUtc = DateTime.UtcNow
		};

		_dbContext.InboxMessages.Add(message);
		await _dbContext.SaveChangesAsync(ct);
		return message;
	}

	/// <summary>
	/// 查询所有 Inbox 消息
	/// </summary>
	public async Task<List<InboxMessage>> GetAllInboxMessagesAsync(CancellationToken ct = default)
	{
		return await _dbContext.InboxMessages
			.OrderByDescending(m => m.ReceivedAtUtc)
			.ToListAsync(ct);
	}

	/// <summary>
	/// 检查 Inbox 消息是否存在
	/// </summary>
	public async Task<bool> CheckInboxExistsAsync(Guid messageId, string consumer, CancellationToken ct = default)
	{
		return await _dbContext.InboxMessages
			.AnyAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
	}

	/// <summary>
	/// 标记 Inbox 消息为已处理
	/// </summary>
	public async Task<bool> MarkInboxAsProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default)
	{
		var message = await _dbContext.InboxMessages
			.FirstOrDefaultAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
		if (message is null) return false;

		message.ProcessedAtUtc = DateTime.UtcNow;
		await _dbContext.SaveChangesAsync(ct);
		return true;
	}

	/// <summary>
	/// 清理所有测试数据
	/// </summary>
	public async Task<int> ClearAllAsync(CancellationToken ct = default)
	{
		var outboxCount = await _dbContext.OutboxMessages.ExecuteDeleteAsync(ct);
		var inboxCount = await _dbContext.InboxMessages.ExecuteDeleteAsync(ct);
		return outboxCount + inboxCount;
	}
}
