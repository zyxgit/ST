using Microsoft.EntityFrameworkCore;

namespace ST.Infra.EntityFramework.OperationLogs;

public interface IOperationLogDbContext
{
	DbSet<OperationLog> OperationLogs { get; }

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

