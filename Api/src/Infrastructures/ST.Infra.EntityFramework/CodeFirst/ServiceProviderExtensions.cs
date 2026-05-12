using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ST.Infra.Repository.Interface;

namespace ST.Infra.EntityFramework.CodeFirst;

public static class ServiceProviderExtensions
{
	public static async Task ExecuteCodeFirstExecutorsAsync(this IServiceProvider serviceProvider)
	{
		var executors = serviceProvider.GetServices<ICodeFirstExecutor>().ToList();
		foreach (var executor in executors)
		{
			await executor.ExecuteAsync(serviceProvider);
		}
	}

	public static Task ExecuteCodeFirstExecutorsAsync(this IHost host)
	{
		return host.Services.ExecuteCodeFirstExecutorsAsync();
	}
}
