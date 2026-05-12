namespace ST.Infra.Tasks.Abstractions;

public interface IBackgroundTaskScheduler
{
	/// <summary>
	/// 立即执行（Best-effort，不阻塞请求）
	/// </summary>
	string Enqueue(Func<CancellationToken, Task> job);

	/// <summary>
	/// 延时执行（需要调度持久化）
	/// </summary>
	string Schedule(Func<CancellationToken, Task> job, TimeSpan delay);

	/// <summary>
	/// 定时执行（Cron）
	/// </summary>
	string Recurring(string jobId, Func<CancellationToken, Task> job, string cron);

	/// <summary>
	/// 移除定时任务
	/// </summary>
	void Remove(string jobId);
}
