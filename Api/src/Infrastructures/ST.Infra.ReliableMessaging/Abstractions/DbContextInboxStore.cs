using Microsoft.EntityFrameworkCore;

namespace ST.Infra.ReliableMessaging.Abstractions;

/// <summary>
/// 基于任意 DbContext 的 Inbox 存储实现。
/// 适用于业务服务将 Inbox 消息与业务数据写入同一 DbContext（同一事务）的场景。
/// </summary>
/// <typeparam name="TDbContext">包含 DbSet&lt;InboxMessage&gt; 的 DbContext 类型。</typeparam>
public sealed class DbContextInboxStore<TDbContext> : IInboxStore
	where TDbContext : Microsoft.EntityFrameworkCore.DbContext
{
	private readonly TDbContext _dbContext;

	public DbContextInboxStore(TDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<bool> ExistsAsync(Guid messageId, string consumer, CancellationToken ct = default)
	{
		return await _dbContext.Set<InboxMessage>()
			.AnyAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
	}

	public void Add(InboxMessage message)
	{
		_dbContext.Set<InboxMessage>().Add(message);
	}

	public async Task MarkAsProcessedAsync(Guid messageId, string consumer, CancellationToken ct = default)
	{
		var message = await _dbContext.Set<InboxMessage>()
			.FirstOrDefaultAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
		if (message is not null)
		{
			message.ProcessedAtUtc = DateTime.UtcNow;
		}
	}

	public async Task MarkAsFailedAsync(Guid messageId, string consumer, string error, CancellationToken ct = default)
	{
		var message = await _dbContext.Set<InboxMessage>()
			.FirstOrDefaultAsync(m => m.MessageId == messageId && m.Consumer == consumer, ct);
		if (message is not null)
		{
			message.ErrorMessage = error;
			message.RetryCount++;
		}
	}
}
