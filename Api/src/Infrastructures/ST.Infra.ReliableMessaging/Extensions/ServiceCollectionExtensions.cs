using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ST.Infra.ReliableMessaging.Abstractions;

namespace ST.Infra.ReliableMessaging.Extensions;

/// <summary>
/// 可靠消息基础设施 DI 注册扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 注册可靠消息基础设施（ReliableMessagingDbContext、IOutboxStore、IInboxStore）。
	/// </summary>
	public static IServiceCollection AddReliableMessaging(
		this IServiceCollection services,
		IConfiguration configuration,
		string connectionStringName = "DefaultConnection")
	{
		services.AddDbContext<DbContext.ReliableMessagingDbContext>(options =>
		{
			var connectionString = configuration.GetConnectionString(connectionStringName);
			options.UseNpgsql(connectionString);
		});

		services.AddScoped<IOutboxStore, EfOutboxStore>();
		services.AddScoped<IInboxStore, EfInboxStore>();

		return services;
	}

	/// <summary>
	/// 注册 Outbox Publisher 后台服务（RabbitMQ 实现）。
	/// 需要先调用 <see cref="AddReliableMessaging(IServiceCollection, IConfiguration, string)"/>。
	/// </summary>
	/// <param name="services">服务集合</param>
	/// <param name="configuration">配置</param>
	/// <param name="sectionName">配置节点名称，默认 OutboxPublisher</param>
	public static IServiceCollection AddOutboxPublisher(
		this IServiceCollection services,
		IConfiguration configuration,
		string sectionName = OutboxPublisherOptions.SectionName)
	{
		var options = new OutboxPublisherOptions();
		configuration.GetSection(sectionName).Bind(options);

		// 尝试从 ConnectionStrings 读取 RabbitMQ 连接串
		ApplyConnectionString(configuration, options);

		services.TryAddSingleton(options);
		services.TryAddSingleton<IOutboxPublisher, RabbitMqOutboxPublisher>();
		services.AddHostedService<OutboxPublisherHostedService>();

		return services;
	}

	/// <summary>
	/// 注册 Outbox Publisher 后台服务（使用自定义配置）。
	/// </summary>
	public static IServiceCollection AddOutboxPublisher(
		this IServiceCollection services,
		Action<OutboxPublisherOptions> configure)
	{
		var options = new OutboxPublisherOptions();
		configure(options);

		services.TryAddSingleton(options);
		services.TryAddSingleton<IOutboxPublisher, RabbitMqOutboxPublisher>();
		services.AddHostedService<OutboxPublisherHostedService>();

		return services;
	}

	private static void ApplyConnectionString(IConfiguration configuration, OutboxPublisherOptions options)
	{
		var connectionStringName = options.ConnectionStringName;
		var connectionString = configuration.GetConnectionString(connectionStringName);
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return;
		}

		try
		{
			var uri = new Uri(connectionString, UriKind.Absolute);
			options.HostName = uri.Host;
			options.Port = uri.IsDefaultPort ? 5672 : uri.Port;
			options.UserName = Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[0]);

			var userInfo = uri.UserInfo.Split(':', 2);
			options.Password = userInfo.Length > 1
				? Uri.UnescapeDataString(userInfo[1])
				: string.Empty;

			var path = uri.AbsolutePath.Trim('/');
			options.VirtualHost = string.IsNullOrWhiteSpace(path) ? "/" : Uri.UnescapeDataString(path);
		}
		catch
		{
			// 连接字符串格式不合法，忽略，使用默认值
		}
	}
}
