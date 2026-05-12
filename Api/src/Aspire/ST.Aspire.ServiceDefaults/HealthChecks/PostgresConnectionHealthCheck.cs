using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class PostgresConnectionHealthCheck : IHealthCheck
{
	private readonly string? _connectionString;

	public PostgresConnectionHealthCheck(IConfiguration configuration)
	{
		var connectionStringName = configuration["Database:ConnectionStringName"] ?? "Default";
		_connectionString = configuration.GetConnectionString(connectionStringName)
			?? configuration["Database:ConnectionString"];
	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(_connectionString))
		{
			return HealthCheckResult.Healthy("Database not configured");
		}

		try
		{
			await using var conn = new NpgsqlConnection(_connectionString);
			await conn.OpenAsync(cancellationToken);

			await using var cmd = new NpgsqlCommand("SELECT 1;", conn);
			var result = await cmd.ExecuteScalarAsync(cancellationToken);
			return result is 1
				? HealthCheckResult.Healthy("Postgres reachable")
				: HealthCheckResult.Unhealthy($"Unexpected result: {result}");
		}
		catch (Exception ex)
		{
			return HealthCheckResult.Unhealthy("Postgres connection failed", ex);
		}
	}
}

