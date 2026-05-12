using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ST.Infra.Tasks.ImmediateExecution;

public sealed class ImmediateTaskExecutor
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<ImmediateTaskExecutor> _logger;

	public ImmediateTaskExecutor(
		IServiceScopeFactory scopeFactory,
		ILogger<ImmediateTaskExecutor> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	public void Run(Func<CancellationToken, Task> job)
	{
		_ = Task.Run(async () =>
		{
			using var scope = _scopeFactory.CreateScope();
			try
			{
				await job(CancellationToken.None);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Immediate background task failed");
			}
		});
	}
}
