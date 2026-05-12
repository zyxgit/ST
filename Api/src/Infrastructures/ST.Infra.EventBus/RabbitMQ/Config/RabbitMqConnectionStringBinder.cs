using Microsoft.Extensions.Configuration;
using ST.Infra.EventBus.OperationLog;

namespace ST.Infra.EventBus.RabbitMQ.Config;

public static class RabbitMqConnectionStringBinder
{
	private const string DefaultConnectionStringName = "rabbitmq";

	public static void ApplyReference(
		IConfiguration configuration,
		RabbitMqEventBusOptions options,
		string? connectionStringName = null)
	{
		if (!TryParse(configuration, connectionStringName, out var reference))
		{
			return;
		}

		Apply(reference, options);
	}

	public static void ApplyReference(
		IConfiguration configuration,
		RabbitMqOperationLogOptions options,
		string? connectionStringName = null)
	{
		if (!TryParse(configuration, connectionStringName, out var reference))
		{
			return;
		}

		Apply(reference, options);
	}

	private static bool TryParse(
		IConfiguration configuration,
		string? connectionStringName,
		out RabbitMqReference reference)
	{
		reference = default;

		var effectiveConnectionStringName = connectionStringName
			?? configuration["RabbitMQ:ConnectionStringName"]
			?? DefaultConnectionStringName;

		var connectionString = configuration.GetConnectionString(effectiveConnectionStringName);
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return false;
		}

		var uri = new Uri(connectionString, UriKind.Absolute);
		reference = new RabbitMqReference(
			uri.Host,
			uri.IsDefaultPort ? 5672 : uri.Port,
			Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[0]),
			GetPassword(uri),
			GetVirtualHost(uri));

		return true;
	}

	private static void Apply(RabbitMqReference reference, RabbitMqEventBusOptions options)
	{
		options.HostName = reference.HostName;
		options.Port = reference.Port;
		options.UserName = reference.UserName;
		options.Password = reference.Password;
		options.VirtualHost = reference.VirtualHost;
	}

	private static void Apply(RabbitMqReference reference, RabbitMqOperationLogOptions options)
	{
		options.HostName = reference.HostName;
		options.Port = reference.Port;
		options.UserName = reference.UserName;
		options.Password = reference.Password;
		options.VirtualHost = reference.VirtualHost;
	}

	private static string GetPassword(Uri uri)
	{
		var userInfo = uri.UserInfo.Split(':', 2);
		return userInfo.Length > 1
			? Uri.UnescapeDataString(userInfo[1])
			: string.Empty;
	}

	private static string GetVirtualHost(Uri uri)
	{
		var path = uri.AbsolutePath.Trim('/');
		return string.IsNullOrWhiteSpace(path)
			? "/"
			: Uri.UnescapeDataString(path);
	}

	private readonly record struct RabbitMqReference(
		string HostName,
		int Port,
		string UserName,
		string Password,
		string VirtualHost);
}
