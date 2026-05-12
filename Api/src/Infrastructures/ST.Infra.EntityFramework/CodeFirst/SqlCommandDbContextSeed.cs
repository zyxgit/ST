using Microsoft.EntityFrameworkCore;

namespace ST.Infra.EntityFramework.CodeFirst;

internal sealed class SqlCommandDbContextSeed<TContext> : IDbContextSeed<TContext> where TContext : DbContext
{
	private readonly string _sql;

	public SqlCommandDbContextSeed(string sql, string? name, int order)
	{
		_sql = sql;
		Name = string.IsNullOrWhiteSpace(name) ? "sql-command" : name;
		Order = order;
	}

	public string Name { get; }

	public int Order { get; }

	public Task SeedAsync(TContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
	{
		return dbContext.Database.ExecuteSqlRawAsync(_sql, cancellationToken);
	}
}
