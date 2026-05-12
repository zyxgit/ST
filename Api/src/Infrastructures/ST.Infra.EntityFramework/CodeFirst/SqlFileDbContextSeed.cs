using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ST.Infra.EntityFramework.CodeFirst;

internal sealed class SqlFileDbContextSeed<TContext> : IDbContextSeed<TContext> where TContext : DbContext
{
	private readonly string _path;

	public SqlFileDbContextSeed(string path, string? name, int order)
	{
		_path = path;
		Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileName(path) : name;
		Order = order;
	}

	public string Name { get; }

	public int Order { get; }

	public async Task SeedAsync(TContext dbContext, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
	{
		var hostEnvironment = serviceProvider.GetService<IHostEnvironment>();
		var resolvedPath = ResolvePath(hostEnvironment?.ContentRootPath, _path);
		var sql = await File.ReadAllTextAsync(resolvedPath, cancellationToken);
		if (string.IsNullOrWhiteSpace(sql))
		{
			return;
		}

		await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
	}

	private static string ResolvePath(string? contentRootPath, string path)
	{
		if (Path.IsPathRooted(path) && File.Exists(path))
		{
			return path;
		}

		var candidates = new List<string>();
		if (!string.IsNullOrWhiteSpace(contentRootPath))
		{
			candidates.Add(Path.Combine(contentRootPath, path));
		}

		candidates.Add(Path.Combine(AppContext.BaseDirectory, path));

		var match = candidates.FirstOrDefault(File.Exists);
		if (match is not null)
		{
			return match;
		}

		throw new FileNotFoundException($"SQL 种子文件不存在: {path}");
	}
}
