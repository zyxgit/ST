namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// 基于 EF Core 的 Inbox 存储实现。
/// </summary>
public sealed class EfInboxStore : IInboxStore
{
	private readonly DbContext.ReliableMessagingDbContext _dbContext;

	public EfInboxStore(DbContext.ReliableMessagingDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<bool> ExistsAsync(Guid messageId, string consumer, CancellationToken ct = default)
	{
		return await _dbContext.InboxMessages
			.AnyAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
	}

	public void Add(InboxMessage message)
	{
		_dbContext.InboxMessages.Add(message);
	}

	public async Task MarkAsProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default)
	{
		var message = await _dbContext.InboxMessages
			.FirstOrDefaultAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
		if (message is not null)
		{
			message.ProcessedAtUtc = DateTime.UtcNow;
		}
	}

	public async Task MarkAsFailedAsync(Guid messageId, string consumer, string error, CancellationToken ct = default)
	{
		var message = await _dbContext.InboxMessages
			.FirstOrDefaultAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
		if (message is not null)
		{
			message.ErrorMessage = error;
			message.RetryCount++;
		}
	}
}
