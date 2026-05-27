using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using ST.Infra.Repository.Interface;
using ST.Shared.Const;

namespace ST.Infra.EntityFramework.CodeFirst;

public sealed class CodeFirstExecutor<TContext> : ICodeFirstExecutor where TContext : DbContext
{
	private readonly IConfiguration _configuration;
	private readonly ILogger<CodeFirstExecutor<TContext>> _logger;
	private int _executed;

	private static string? _productVersion;

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
			await EnsureDatabaseWithMigrationsAsync(dbContext);
			return;
		}

		_logger.LogInformation("DbContext {DbContext} has no migrations. Falling back to relational database creator.", typeof(TContext).Name);

		var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
		var databaseExists = await databaseCreator.ExistsAsync();
		if (!databaseExists)
		{
			_logger.LogInformation("DbContext {DbContext} database does not exist. Creating database and tables.", typeof(TContext).Name);
			await dbContext.Database.EnsureCreatedAsync();
		}
		else if (!await databaseCreator.HasTablesAsync())
		{
			_logger.LogInformation("DbContext {DbContext} database exists but has no tables. Creating tables from current model.", typeof(TContext).Name);
			await databaseCreator.CreateTablesAsync();
		}
		else
		{
			_logger.LogInformation("DbContext {DbContext} database exists and already has tables. Skipping table creation because there are no migrations to apply.", typeof(TContext).Name);
		}

		// 外键已在 OnModelCreating.ApplyNoForeignKeys() 中从模型层移除，
		// EnsureCreatedAsync / CreateTablesAsync 不再生成 FOREIGN KEY 约束。
	}

	private async Task EnsureDatabaseWithMigrationsAsync(TContext dbContext)
	{
		var historyRepo = dbContext.GetService<IHistoryRepository>();
		var historyExists = await historyRepo.ExistsAsync();
		var allMigrations = dbContext.Database.GetMigrations().ToList();

		if (historyExists)
		{
			var appliedCount = (await historyRepo.GetAppliedMigrationsAsync()).Count;
			if (appliedCount >= allMigrations.Count)
			{
				_logger.LogInformation(
					"DbContext {DbContext} has {Applied}/{Total} migration(s) applied. Nothing to do.",
					typeof(TContext).Name, appliedCount, allMigrations.Count);
				return;
			}

			// 部分迁移已应用，直接继续
			_logger.LogInformation(
				"DbContext {DbContext} is applying migrations. (applied={Applied}/{Total})",
				typeof(TContext).Name, appliedCount, allMigrations.Count);

			try
			{
				await dbContext.Database.MigrateAsync();
			}
			catch (PostgresException ex) when (ex.SqlState == "42P07")
			{
				await RecoverFromTableExistsAsync(dbContext, historyRepo, allMigrations);
			}

			return;
		}

		// 无迁移历史：检查是否已有表（手动建表但未跑迁移）
		var relationalCreator = dbContext.GetService<IRelationalDatabaseCreator>();
		if (await relationalCreator.HasTablesAsync())
		{
			_logger.LogInformation(
				"DbContext {DbContext} has {Count} migration(s) and existing tables but no migration history. Seeding history.",
				typeof(TContext).Name, allMigrations.Count);
			await SeedMigrationHistoryAsync(dbContext, historyRepo, allMigrations);
			return;
		}

		// 全新数据库，直接 Migrate
		_logger.LogInformation(
			"DbContext {DbContext} is applying migrations to empty database. (count={Count})",
			typeof(TContext).Name, allMigrations.Count);

		try
		{
			await dbContext.Database.MigrateAsync();
		}
		catch (PostgresException ex) when (ex.SqlState == "42P07")
		{
			await RecoverFromTableExistsAsync(dbContext, historyRepo, allMigrations);
		}
	}

	/// <summary>
	/// 迁移历史表种子（去重：仅插入尚未记录的迁移 ID）。
	/// </summary>
	private async Task SeedMigrationHistoryAsync(
		TContext dbContext,
		IHistoryRepository historyRepo,
		List<string> allMigrations)
	{
		if (!await historyRepo.ExistsAsync())
		{
			var createScript = historyRepo.GetCreateIfNotExistsScript();
			if (!string.IsNullOrEmpty(createScript))
				await dbContext.Database.ExecuteSqlRawAsync(createScript);
		}

		var productVersion = GetProductVersion();
		foreach (var migrationId in allMigrations)
		{
			var row = new HistoryRow(migrationId, productVersion);
			var insertScript = historyRepo.GetInsertScript(row);
			await dbContext.Database.ExecuteSqlRawAsync(insertScript);
		}
	}

	/// <summary>
	/// 42P07（表已存在）恢复：播种历史表后让 MigrateAsync 可继续。
	/// </summary>
	private async Task RecoverFromTableExistsAsync(
		TContext dbContext,
		IHistoryRepository historyRepo,
		List<string> allMigrations)
	{
		_logger.LogWarning("MigrateAsync failed with 42P07 (table already exists). Seeding migration history.");

		await SeedMigrationHistoryAsync(dbContext, historyRepo, allMigrations);

		_logger.LogInformation(
			"DbContext {DbContext} recovered from 42P07 by seeding {Count} migration(s).",
			typeof(TContext).Name, allMigrations.Count);
	}

	private static string GetProductVersion()
	{
		return _productVersion ??= typeof(DbContext).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			?.InformationalVersion ?? "10.0.0";
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
