using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ST.Infra.EntityFramework.CodeFirst;

public sealed class DbContextSeedBuilder<TContext> where TContext : DbContext
{
	private readonly IServiceCollection _services;

	public DbContextSeedBuilder(IServiceCollection services)
	{
		_services = services;
	}

	public DbContextSeedBuilder<TContext> Add<TSeed>() where TSeed : class, IDbContextSeed<TContext>
	{
		_services.AddTransient<IDbContextSeed<TContext>, TSeed>();
		return this;
	}

	public DbContextSeedBuilder<TContext> AddSql(string sql, string? name = null, int order = 0)
	{
		_services.AddSingleton<IDbContextSeed<TContext>>(new SqlCommandDbContextSeed<TContext>(sql, name, order));
		return this;
	}

	public DbContextSeedBuilder<TContext> AddSqlFile(string path, string? name = null, int order = 0)
	{
		_services.AddSingleton<IDbContextSeed<TContext>>(new SqlFileDbContextSeed<TContext>(path, name, order));
		return this;
	}

	public DbContextSeedBuilder<TContext> AddDelegate(
		Func<TContext, IServiceProvider, CancellationToken, Task> seed,
		string? name = null,
		int order = 0)
	{
		_services.AddSingleton<IDbContextSeed<TContext>>(new DelegateDbContextSeed<TContext>(seed, name, order));
		return this;
	}
}
