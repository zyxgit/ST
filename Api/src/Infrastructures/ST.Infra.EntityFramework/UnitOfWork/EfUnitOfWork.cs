using Microsoft.EntityFrameworkCore;
using ST.Infra.Repository.Interface;

namespace ST.Infra.EntityFramework.UnitOfWork;

internal sealed class EfUnitOfWork : IUnitOfWork
{
	private readonly DbContext _dbContext;

	public EfUnitOfWork(DbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task ExecuteAsync(Func<Task> action)
	{
		var strategy = _dbContext.Database.CreateExecutionStrategy();

		await strategy.ExecuteAsync(async () =>
		{
			await using var tx = await _dbContext.Database.BeginTransactionAsync();
			try
			{
				await action();
				await _dbContext.SaveChangesAsync();
				await tx.CommitAsync();
			}
			catch
			{
				await tx.RollbackAsync();
				throw;
			}
		});
	}

}
