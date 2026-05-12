using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ST.Infra.Redis.Cache;
using ST.Infra.Redis.Config;
using ST.Infra.Redis.Provider;

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
}
