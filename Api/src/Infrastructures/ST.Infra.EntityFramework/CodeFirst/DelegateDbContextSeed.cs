using Microsoft.EntityFrameworkCore;

namespace ST.Infra.EntityFramework.CodeFirst;

internal sealed class DelegateDbContextSeed<TContext> : IDbContextSeed<TContext> where TContext : DbContext
{
	private readonly Func<TContext, IServiceProvider, CancellationToken, Task> _seed;

	public DelegateDbContextSeed(
		Func<TContext, IServiceProvider, CancellationToken, Task> seed,
		string? name,
		int order)
	{
		_seed = seed;
		Name = string.IsNullOrWhiteSpace(name) ? "ef-code" : name;
		Order = order;
	}

	public string Name { get; }

	public int Order { get; }

	public Task SeedAsync(TContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
	{
		return _seed(dbContext, serviceProvider, cancellationToken);
	}
}
