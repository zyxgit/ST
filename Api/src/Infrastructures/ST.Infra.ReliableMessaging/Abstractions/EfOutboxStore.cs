namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// 基于 EF Core 的 Outbox 存储实现。
/// </summary>
public sealed class EfOutboxStore : IOutboxStore
{
	private readonly DbContext.ReliableMessagingDbContext _dbContext;

	public EfOutboxStore(DbContext.ReliableMessagingDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public void Add(OutboxMessage message)
	{
		_dbContext.OutboxMessages.Add(message);
	}

	public void AddRange(IEnumerable<OutboxMessage> messages)
	{
		_dbContext.OutboxMessages.AddRange(messages);
	}

	public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await _dbContext.OutboxMessages
			.Where(m => m.Status == OutboxStatus.Pending
				&& (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
			.OrderBy(m => m.OccurredAtUtc)
			.Take(batchSize)
			.ToListAsync(ct);
	}

	public async Task<IReadOnlyList<OutboxMessage>> GetRetryableAsync(int batchSize, CancellationToken ct = default)
	{
		var now = DateTime.UtcNow;
		return await _dbContext.OutboxMessages
			.Where(m => (m.Status == OutboxStatus.Pending || m.Status == OutboxStatus.Failed)
				&& (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now))
			.OrderBy(m => m.OccurredAtUtc)
			.Take(batchSize)
			.ToListAsync(ct);
	}

	public async Task MarkAsSentAsync(Guid messageId, CancellationToken ct = default)
	{
		var message = await _dbContext.OutboxMessages.FindAsync([messageId], ct);
		if (message is not null)
		{
			message.Status = OutboxStatus.Sent;
			message.SentAtUtc = DateTime.UtcNow;
		}
	}

	public async Task MarkAsFailedAsync(Guid messageId, string error, DateTime nextRetryAtUtc, CancellationToken ct = default)
	{
		var message = await _dbContext.OutboxMessages.FindAsync([messageId], ct);
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
