namespace ST.Shared.OperationLog;

public interface IOperationLogDispatcher
{
	ValueTask EnqueueAsync(OperationLogEntry entry, CancellationToken cancellationToken = default);
}

