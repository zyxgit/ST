using Microsoft.Extensions.Configuration;

namespace ST.Infra.EntityFramework.Configuration;

public sealed record DatabaseConnectionInfo(
	string Provider,
	string ConnectionString
);

public static class DatabaseConnectionInfoResolver
{
	// 新约定（推荐）：
	// - Database:Provider
	// - Database:ConnectionString、Database:ConnectionStringName 指向的 ConnectionStrings:{name} 或 ConnectionStrings:Default
	//
	// 旧约定（仍兼容，便于渐进迁移）：
	// - DbConnectionString:DbType + DbConnectionString:{DbType}
	// - ConnectionString:DbType + ConnectionString:{DbType}
	public static DatabaseConnectionInfo Resolve(IConfiguration configuration)
	{
		var provider =
			configuration["Database:Provider"]
			?? configuration["Database:DbType"];

		var connectionStringName = configuration["Database:ConnectionStringName"];
		var connectionString =
			configuration["Database:ConnectionString"]
			?? (!string.IsNullOrWhiteSpace(connectionStringName)
				? configuration.GetConnectionString(connectionStringName)
				: null)
			?? configuration.GetConnectionString("Default");

		if (!string.IsNullOrWhiteSpace(connectionString))
		{
			return new DatabaseConnectionInfo(
				provider ?? "Npgsql",
				connectionString);
		}

		// 旧约定：DbConnectionString
		var legacyDbType = configuration["DbConnectionString:DbType"];
		if (!string.IsNullOrWhiteSpace(legacyDbType))
		{
			var legacyConn = configuration[$"DbConnectionString:{legacyDbType}"];
			if (!string.IsNullOrWhiteSpace(legacyConn))
			{
				return new DatabaseConnectionInfo(legacyDbType, legacyConn);
			}
		}

		// 旧约定：ConnectionString
		var legacyConnDbType = configuration["ConnectionString:DbType"];
		if (!string.IsNullOrWhiteSpace(legacyConnDbType))
		{
			var legacyConn = configuration[$"ConnectionString:{legacyConnDbType}"];
			if (!string.IsNullOrWhiteSpace(legacyConn))
			{
				return new DatabaseConnectionInfo(legacyConnDbType, legacyConn);
			}
		}

		throw new InvalidOperationException(
			"数据库连接未配置。请设置 'Database:ConnectionString'、'Database:ConnectionStringName' 指向的连接字符串，或 'ConnectionStrings:Default'。");
	}
}
