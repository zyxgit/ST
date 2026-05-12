using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ST.Infra.Tasks.PersistentScheduler;

public sealed class PersistentTaskExecutor
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<PersistentTaskExecutor> _logger;

	public PersistentTaskExecutor(
		IServiceScopeFactory scopeFactory,
		ILogger<PersistentTaskExecutor> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	public async Task ExecuteAsync(Func<CancellationToken, Task> job)
	{
		using var scope = _scopeFactory.CreateScope();
		try
		{
			await job(CancellationToken.None);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Persistent background task failed");
			throw;
		}
	}
}
