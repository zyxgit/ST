using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ST.Infra.EntityFramework.CodeFirst;
using ST.Infra.EntityFramework.Configuration;
using ST.Infra.EntityFramework.Extensions;
using ST.Infra.Repository.Interface;

namespace ST.Infra.EntityFramework.Npgsql.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddNpgsqlDbContextFromConfig<TContext>(this IServiceCollection services)
		where TContext : DbContext
	{
		return services.AddNpgsqlDbContextFromConfig<TContext>(null);
	}

	public static IServiceCollection AddNpgsqlDbContextFromConfig<TContext>(
		this IServiceCollection services,
		Action<DbContextSeedBuilder<TContext>>? configureSeeds)
		where TContext : DbContext
	{
		services.AddDbContext<TContext>((sp, options) =>
		{
			var configuration = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
			var info = DatabaseConnectionInfoResolver.Resolve(configuration);

			if (!IsPostgresProvider(info.Provider))
			{
				throw new InvalidOperationException(
					$"数据库 Provider '{info.Provider}' 不受 Npgsql 支持，请将 'Database:Provider' 设置为 'Npgsql' 或 'PostgreSQL'。");
			}

			options.UseNpgsql(info.ConnectionString);
		});

		services.AddEfInfrastructure<TContext>();
		services.TryAddEnumerable(ServiceDescriptor.Singleton<ICodeFirstExecutor, CodeFirstExecutor<TContext>>());

		if (configureSeeds is not null)
		{
			configureSeeds(new DbContextSeedBuilder<TContext>(services));
		}

		return services;
	}

	private static bool IsPostgresProvider(string provider)
	{
		if (string.IsNullOrWhiteSpace(provider))
		{
			return true;
		}

		return provider.Equals("npgsql", StringComparison.OrdinalIgnoreCase)
			|| provider.Equals("postgres", StringComparison.OrdinalIgnoreCase)
			|| provider.Equals("postgresql", StringComparison.OrdinalIgnoreCase);
	}
}
