using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.EventBus.RabbitMQ.Config;
using ST.Infra.EventBus.RabbitMQ.Internal;

namespace ST.Infra.EventBus.RabbitMQ.Extensions;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 配置节点默认：RabbitMQ:EventBus
	/// </summary>
	public static IServiceCollection AddRabbitMqEventBus(
		this IServiceCollection services,
		IConfiguration configuration,
		string sectionName = "RabbitMQ:EventBus")
	{
		var options = new RabbitMqEventBusOptions();
		configuration.GetSection(sectionName).Bind(options);
		RabbitMqConnectionStringBinder.ApplyReference(configuration, options);

		if (string.IsNullOrWhiteSpace(options.QueueName))
		{
			options.QueueName = GetDefaultQueueName();
		}

		services.AddSingleton(options);
		services.AddSingleton<IRabbitMqPersistentConnection, RabbitMqPersistentConnection>();
		services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
		services.AddSingleton<IEventBus, RabbitMqEventBus>();

		return services;
	}

	private static string GetDefaultQueueName()
	{
		var entryName = Assembly.GetEntryAssembly()?.GetName().Name;
		if (!string.IsNullOrWhiteSpace(entryName))
		{
			return entryName;
		}

		return $"st.eventbus.{Environment.MachineName}".ToLowerInvariant();
	}
}

