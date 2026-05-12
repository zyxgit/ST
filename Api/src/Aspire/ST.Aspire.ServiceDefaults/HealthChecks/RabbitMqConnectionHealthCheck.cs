using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class RabbitMqConnectionHealthCheck : IHealthCheck
{
	private readonly IConfiguration _configuration;

	public RabbitMqConnectionHealthCheck(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		if (!TryGetReference(out var options) && !TryGetFirstConfiguredSection(out options))
		{
			return HealthCheckResult.Healthy("RabbitMQ not configured");
		}

		try
		{
			var factory = new ConnectionFactory
			{
				HostName = options.HostName,
				Port = options.Port,
				UserName = options.UserName,
				Password = options.Password,
				VirtualHost = options.VirtualHost
			};

			using var conn = await factory.CreateConnectionAsync(cancellationToken);
			// CreateConnection 成功即认为可达（健康检查目的通常只要连通性）
			return HealthCheckResult.Healthy("RabbitMQ reachable");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("RabbitMQ connection failed", ex);
		}
	}

	private bool TryGetReference(out RabbitMqSectionOptions options)
	{
		options = default!;

		var connectionStringName = _configuration["RabbitMQ:ConnectionStringName"] ?? "rabbitmq";
		var connectionString = _configuration.GetConnectionString(connectionStringName);
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			return false;
		}

		var uri = new Uri(connectionString, UriKind.Absolute);
		var userInfo = uri.UserInfo.Split(':', 2);
		options = new RabbitMqSectionOptions
		{
			HostName = uri.Host,
			Port = uri.IsDefaultPort ? 5672 : uri.Port,
			UserName = Uri.UnescapeDataString(userInfo[0]),
			Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
			VirtualHost = GetVirtualHost(uri)
		};

		return true;
	}

	private static string GetVirtualHost(Uri uri)
	{
		var path = uri.AbsolutePath.Trim('/');
		return string.IsNullOrWhiteSpace(path) ? "/" : Uri.UnescapeDataString(path);
	}

	private bool TryGetFirstConfiguredSection(out RabbitMqSectionOptions options)
	{
		options = default!;

		// 优先 EventBus，其次 OperationLog
		if (TryBindSection("RabbitMQ:EventBus", out options))
		{
			return true;
		}

		return TryBindSection("RabbitMQ:OperationLog", out options);
	}

	private bool TryBindSection(string sectionPath, out RabbitMqSectionOptions options)
	{
		options = new RabbitMqSectionOptions
		{
			HostName = _configuration[$"{sectionPath}:HostName"] ?? string.Empty,
			Port = _configuration.GetValue<int?>($"{sectionPath}:Port") ?? 5672,
			UserName = _configuration[$"{sectionPath}:UserName"] ?? string.Empty,
			Password = _configuration[$"{sectionPath}:Password"] ?? string.Empty,
			VirtualHost = _configuration[$"{sectionPath}:VirtualHost"] ?? "/"
		};

		return !string.IsNullOrWhiteSpace(options.HostName);
	}

	private sealed class RabbitMqSectionOptions
	{
		public string HostName { get; set; } = string.Empty;
		public int Port { get; set; }
		public string UserName { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
		public string VirtualHost { get; set; } = "/";
	}
}

