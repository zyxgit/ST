using System;
using System.Collections.Generic;
using System.Text;
using Hangfire;
using ST.Infra.Tasks.Abstractions;
using ST.Infra.Tasks.ImmediateExecution;

namespace ST.Infra.Tasks.PersistentScheduler;

public sealed class HangfireBackgroundTaskScheduler
	: IBackgroundTaskScheduler
{
	private readonly ImmediateTaskExecutor _immediate;
	private readonly PersistentTaskExecutor _persistent;

	public HangfireBackgroundTaskScheduler(
		ImmediateTaskExecutor immediate,
		PersistentTaskExecutor persistent)
	{
		_immediate = immediate;
		_persistent = persistent;
	}

	public string Enqueue(Func<CancellationToken, Task> job)
	{
		_immediate.Run(job);
		return Guid.NewGuid().ToString("N");
	}

	public string Schedule(Func<CancellationToken, Task> job, TimeSpan delay)
	{
		return BackgroundJob.Schedule(
			() => _persistent.ExecuteAsync(job),
			delay);
	}

	public string Recurring(string jobId, Func<CancellationToken, Task> job, string cron)
	{
		RecurringJob.AddOrUpdate(
			jobId,
			() => _persistent.ExecuteAsync(job),
			cron);

		return jobId;
	}

	public void Remove(string jobId)
	{
		RecurringJob.RemoveIfExists(jobId);
	}
}
