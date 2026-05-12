using Microsoft.EntityFrameworkCore;

namespace ST.Infra.EntityFramework.CodeFirst;

public interface IDbContextSeed<TContext> where TContext : DbContext
{
	string Name { get; }

	int Order { get; }

	Task SeedAsync(TContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
