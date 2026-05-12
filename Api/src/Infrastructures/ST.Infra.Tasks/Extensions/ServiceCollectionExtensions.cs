using Microsoft.Extensions.DependencyInjection;
using ST.Infra.Tasks.Abstractions;
using ST.Infra.Tasks.ImmediateExecution;
using ST.Infra.Tasks.PersistentScheduler;

namespace ST.Infra.Tasks.Extensions;

public static class ServiceCollectionExtensions
{

	public static IServiceCollection AddInfraTasks(
		this IServiceCollection services)
	{
		services.AddSingleton<ImmediateTaskExecutor>();
		services.AddSingleton<PersistentTaskExecutor>();
		services.AddSingleton<IBackgroundTaskScheduler, HangfireBackgroundTaskScheduler>();

		return services;
	}

}
