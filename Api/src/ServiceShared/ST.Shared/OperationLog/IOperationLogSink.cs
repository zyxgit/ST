namespace ST.Shared.OperationLog;

public interface IOperationLogSink
{
	string Name { get; }

	ValueTask EnqueueAsync(OperationLogEntry entry, CancellationToken cancellationToken = default);
}
