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
		if (string.IsNullOrWhiteSpace(connectionStringName))
		{
			var redisOptions = new RedisOptions();
			configuration.GetSection("Redis").Bind(redisOptions);
			services.AddSingleton(redisOptions);
		}
		else
		{
			var connectionString = configuration.GetConnectionString(connectionStringName) ?? string.Empty;
			var redisOptions = new RedisOptions(connectionString);
			services.AddSingleton(redisOptions);
		}

		services.AddSingleton<IRedisClient, RedisClientFactory>();
		services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
		return services;
	}
}
