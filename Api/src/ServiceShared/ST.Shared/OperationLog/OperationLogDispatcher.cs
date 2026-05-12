using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ST.Shared.OperationLog;

public sealed class OperationLogDispatcher : IOperationLogDispatcher
{
	private readonly IEnumerable<IOperationLogSink> _sinks;
	private readonly ILogger<OperationLogDispatcher> _logger;
	private readonly IOptions<OperationLogOptions> _options;

	public OperationLogDispatcher(
		IEnumerable<IOperationLogSink> sinks,
		IOptions<OperationLogOptions> options,
		ILogger<OperationLogDispatcher> logger)
	{
		_sinks = sinks;
		_options = options;
		_logger = logger;
	}

	public async ValueTask EnqueueAsync(OperationLogEntry entry, CancellationToken cancellationToken = default)
	{
		var enabled = _options.Value.Sinks;
		var filterEnabled = enabled is { Length: > 0 };

		foreach (var sink in _sinks)
		{
			if (filterEnabled && !enabled.Any(x => x.Equals(sink.Name, StringComparison.OrdinalIgnoreCase)))
			{
				continue;
			}

			try
			{
				await sink.EnqueueAsync(entry, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "OperationLog sink enqueue failed. Sink={SinkType}", sink.GetType().FullName);
			}
		}
	}
}
