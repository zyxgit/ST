using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ST.Infra.Repository.Interface;
using ST.Shared.Const;

namespace ST.Infra.EntityFramework.CodeFirst;

public sealed class CodeFirstExecutor<TContext> : ICodeFirstExecutor where TContext : DbContext
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<CodeFirstExecutor<TContext>> _logger;
	private int _executed;

	public CodeFirstExecutor(
		IConfiguration configuration,
		ILogger<CodeFirstExecutor<TContext>> logger)
	{
		_configuration = configuration;
		_logger = logger;
	}

	public async Task ExecuteAsync(IServiceProvider serviceProvider)
	{
		if (Interlocked.Exchange(ref _executed, 1) == 1)
		{
			return;
		}

		var runMigrations = _configuration.GetValue<bool>(SettingPrefixContants.App_CodeFirst)
			&& _configuration.GetValue<bool>(SettingPrefixContants.App_CodeFirst_IsCreateDatabase);
		var runSeeds = _configuration.GetValue<bool>(SettingPrefixContants.App_DataSeed);
		if (!runMigrations && !runSeeds)
		{
			_logger.LogDebug("DbContext {DbContext} skipped initialization because startup flags are disabled.", typeof(TContext).Name);
			return;
		}

		await using var scope = serviceProvider.CreateAsyncScope();
		var scopedProvider = scope.ServiceProvider;
		var dbContext = scopedProvider.GetRequiredService<TContext>();

		if (runMigrations)
		{
			await EnsureDatabaseAsync(dbContext);
		}

		if (runSeeds)
		{
			await RunSeedsAsync(dbContext, scopedProvider);
		}
	}

	private async Task EnsureDatabaseAsync(TContext dbContext)
	{
		var hasMigrations = dbContext.Database.GetMigrations().Any();
		if (hasMigrations)
		{
			_logger.LogInformation("DbContext {DbContext} is applying migrations.", typeof(TContext).Name);
			await dbContext.Database.MigrateAsync();
			return;
		}

		_logger.LogInformation("DbContext {DbContext} has no migrations. Falling back to relational database creator.", typeof(TContext).Name);

		var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
		var databaseExists = await databaseCreator.ExistsAsync();
		if (!databaseExists)
		{
			_logger.LogInformation("DbContext {DbContext} database does not exist. Creating database and tables.", typeof(TContext).Name);
			await dbContext.Database.EnsureCreatedAsync();
			return;
		}

		var hasTables = await databaseCreator.HasTablesAsync();
		if (!hasTables)
		{
			_logger.LogInformation("DbContext {DbContext} database exists but has no tables. Creating tables from current model.", typeof(TContext).Name);
			await databaseCreator.CreateTablesAsync();
			return;
		}

		_logger.LogInformation("DbContext {DbContext} database exists and already has tables. Skipping table creation because there are no migrations to apply.", typeof(TContext).Name);
	}

	private async Task RunSeedsAsync(TContext dbContext, IServiceProvider serviceProvider)
	{
		var seeds = serviceProvider.GetServices<IDbContextSeed<TContext>>()
			.OrderBy(x => x.Order)
			.ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (seeds.Count == 0)
		{
			_logger.LogInformation("DbContext {DbContext} has no registered seed steps.", typeof(TContext).Name);
			return;
		}

		foreach (var seed in seeds)
		{
			_logger.LogInformation("DbContext {DbContext} is running seed {SeedName}.", typeof(TContext).Name, seed.Name);
			await seed.SeedAsync(dbContext, serviceProvider);
			if (dbContext.ChangeTracker.HasChanges())
			{
				await dbContext.SaveChangesAsync();
			}
			dbContext.ChangeTracker.Clear();
		}
	}
}
