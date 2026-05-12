using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class RedisConnectionHealthCheck : IHealthCheck
{
	private readonly string? _connectionString;

	public RedisConnectionHealthCheck(IConfiguration configuration)
	{
		var connectionStringName = configuration["Redis:ConnectionStringName"] ?? "cache";
		_connectionString = configuration.GetConnectionString(connectionStringName)
			?? configuration["Redis:ConnectionString"];
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_connectionString))
		{
			return HealthCheckResult.Healthy("Redis not configured");
		}

		try
		{
			var options = ConfigurationOptions.Parse(_connectionString);
			// 避免在健康检查中携带无穷重试，使用库的默认重试策略即可
			using var mux = await ConnectionMultiplexer.ConnectAsync(options);
			var db = mux.GetDatabase();
			var ping = await db.PingAsync();
			return ping.TotalMilliseconds >= 0
				? HealthCheckResult.Healthy($"Redis ping: {ping.TotalMilliseconds}ms")
				: HealthCheckResult.Unhealthy("Redis ping returned invalid latency");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("Redis connection failed", ex);
		}
	}
}

