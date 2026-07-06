using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ST.Infra.Redis.Cache;
using ST.Infra.Redis.Config;
using ST.Infra.Redis.Provider;
using ST.Infra.Redis.Inventory;
using ST.Infra.Redis.RateLimiting;

namespace ST.Infra.Redis.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddRedisInfra(this IServiceCollection services, IConfiguration configuration, string? connectionStringName = null)
	{
		var redisOptions = new RedisOptions();
		configuration.GetSection("Redis").Bind(redisOptions);

		var effectiveConnectionStringName = connectionStringName
			?? configuration["Redis:ConnectionStringName"]
			?? "cache";

		var referencedConnectionString = configuration.GetConnectionString(effectiveConnectionStringName);
		if (!string.IsNullOrWhiteSpace(referencedConnectionString))
		{
			redisOptions.ConnectionString = referencedConnectionString;
		}

		services.AddSingleton(redisOptions);

		services.AddSingleton<IRedisClient, RedisClientFactory>();
		services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
		return services;
	}

	/// <summary>
	/// 添加 Redis 分布式限流服务。
	/// </summary>
	public static IServiceCollection AddRedisRateLimiting(this IServiceCollection services)
	{
		services.AddSingleton<IRateLimiter, RedisRateLimiter>();
		return services;
	}

	/// <summary>
	/// 添加 Redis 库存预扣服务。
	/// </summary>
	public static IServiceCollection AddInventoryRedis(this IServiceCollection services)
	{
		services.AddSingleton<IInventoryRedisService, InventoryRedisService>();
		return services;
	}
}
