using Microsoft.EntityFrameworkCore;

namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// 基于任意 DbContext 的 Outbox 存储实现。
/// 适用于业务服务将 Outbox 消息与业务数据写入同一 DbContext（同一事务）的场景。
/// </summary>
/// <typeparam name="TDbContext">包含 DbSet&lt;OutboxMessage&gt; 的 DbContext 类型。</typeparam>
public sealed class DbContextOutboxStore<TDbContext> : IOutboxStore
	where TDbContext : Microsoft.EntityFrameworkCore.DbContext
{
	private readonly TDbContext _dbContext;

	public DbContextOutboxStore(TDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public void Add(OutboxMessage message)
	{
		_dbContext.Set<OutboxMessage>().Add(message);
	}

	public void AddRange(IEnumerable<OutboxMessage> messages)
	{
		_dbContext.Set<OutboxMessage>().AddRange(messages);
	}

	public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await _dbContext.Set<OutboxMessage>()
			.Where(m => m.Status == OutboxStatus.Pending
				&& (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
			.OrderBy(m => m.OccurredAtUtc)
			.Take(batchSize)
			.ToListAsync(ct);
	}

	public async Task<IReadOnlyList<OutboxMessage>> GetRetryableAsync(int batchSize, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await _dbContext.Set<OutboxMessage>()
			.Where(m => (m.Status == OutboxStatus.Pending || m.Status == OutboxStatus.Failed)
				&& (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
			.OrderBy(m => m.OccurredAtUtc)
			.Take(batchSize)
			.ToListAsync(ct);
	}

	public async Task MarkAsSentAsync(Guid messageId, CancellationToken ct = default)
	{
		var message = await _dbContext.Set<OutboxMessage>().FindAsync([messageId], ct);
		if (message is not null)
		{
			message.Status = OutboxStatus.Sent;
			message.SentAtUtc = DateTime.UtcNow;
		}
	}

	public async Task MarkAsFailedAsync(Guid messageId, string error, DateTime nextRetryAtUtc, CancellationToken ct = default)
	{
		var message = await _dbContext.Set<OutboxMessage>().FindAsync([messageId], ct);
		if (message is not null)
		{
			message.Status = OutboxStatus.Failed;
			message.ErrorMessage = error;
			message.RetryCount++;
			message.NextRetryAtUtc = nextRetryAtUtc;
		}
	}

	public async Task SaveChangesAsync(CancellationToken ct = default)
	{
		await _dbContext.SaveChangesAsync(ct);
	}
}
