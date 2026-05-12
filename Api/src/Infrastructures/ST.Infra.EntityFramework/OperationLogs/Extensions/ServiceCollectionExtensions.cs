using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.Shared.OperationLog;

namespace ST.Infra.EntityFramework.OperationLogs.Extensions;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 使用 Channel + BackgroundService 异步写入操作日志。
	/// 需要你的 DbContext 实现 <see cref="IOperationLogDbContext"/>。
	/// 配置节点：OperationLog
	/// </summary>
	public static IServiceCollection AddOperationLogEf<TDbContext>(this IServiceCollection services)
		where TDbContext : DbContext, IOperationLogDbContext
	{
		services.AddSingleton<IOptions<OperationLogOptions>>(sp =>
		{
			var opt = new OperationLogOptions();
			var configuration = sp.GetRequiredService<IConfiguration>();
			configuration.GetSection("OperationLog").Bind(opt);
			return Options.Create(opt);
		});

		services.AddSingleton<OperationLogChannel>();
		services.AddSingleton<IOperationLogSink, EfOperationLogSink>();
		services.AddHostedService<OperationLogWriterHostedService<TDbContext>>();

		return services;
	}

	private sealed class OperationLogChannel
	{
		public OperationLogChannel(IOptions<OperationLogOptions> options)
		{
			var opt = options.Value;
			Channel = System.Threading.Channels.Channel.CreateBounded<OperationLogEntry>(new BoundedChannelOptions(opt.ChannelCapacity)
			{
				SingleReader = true,
				SingleWriter = false,
				FullMode = opt.DropWhenFull ? BoundedChannelFullMode.DropWrite : BoundedChannelFullMode.Wait
			});
		}

		public Channel<OperationLogEntry> Channel { get; }
	}

	private sealed class EfOperationLogSink : IOperationLogSink
	{
		public string Name => "ef";

		private readonly OperationLogChannel _channel;
		private readonly IOptions<OperationLogOptions> _options;
		private readonly ILogger<EfOperationLogSink> _logger;
		private long _dropped;

		public EfOperationLogSink(OperationLogChannel channel, IOptions<OperationLogOptions> options, ILogger<EfOperationLogSink> logger)
		{
			_channel = channel;
			_options = options;
			_logger = logger;
		}

		public ValueTask EnqueueAsync(OperationLogEntry entry, CancellationToken cancellationToken = default)
		{
			var opt = _options.Value;
			if (opt.DropWhenFull && !_channel.Channel.Writer.TryWrite(entry))
			{
				var dropped = Interlocked.Increment(ref _dropped);
				if (dropped % 1000 == 0)
				{
					_logger.LogWarning("OperationLog dropped due to full channel. DroppedCount={DroppedCount}", dropped);
				}

				return ValueTask.CompletedTask;
			}

			return _channel.Channel.Writer.WriteAsync(entry, cancellationToken);
		}
	}

	private sealed class OperationLogWriterHostedService<TDbContext> : BackgroundService
		where TDbContext : DbContext, IOperationLogDbContext
	{
		private readonly OperationLogChannel _channel;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IOptions<OperationLogOptions> _options;
		private readonly ILogger<OperationLogWriterHostedService<TDbContext>> _logger;

		public OperationLogWriterHostedService(
			OperationLogChannel channel,
			IServiceScopeFactory scopeFactory,
			IOptions<OperationLogOptions> options,
			ILogger<OperationLogWriterHostedService<TDbContext>> logger)
		{
			_channel = channel;
			_scopeFactory = scopeFactory;
			_options = options;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var opt = _options.Value;
			var reader = _channel.Channel.Reader;

			var buffer = new List<OperationLogEntry>(Math.Max(1, opt.BatchSize));
			var flushInterval = TimeSpan.FromMilliseconds(Math.Max(50, opt.FlushIntervalMs));

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					if (buffer.Count == 0)
					{
						var readTask = reader.ReadAsync(stoppingToken).AsTask();
						var completed = await Task.WhenAny(readTask, Task.Delay(flushInterval, stoppingToken)).ConfigureAwait(false);
						if (completed == readTask)
						{
							buffer.Add(readTask.Result);
						}
					}

					while (buffer.Count < opt.BatchSize && reader.TryRead(out var item))
					{
						buffer.Add(item);
					}

					if (buffer.Count > 0)
					{
						await FlushAsync(buffer, stoppingToken).ConfigureAwait(false);
						buffer.Clear();
					}
				}
				catch (OperationCanceledException)
				{
					// shutting down
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "OperationLog flush failed.");
					await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
				}
			}
		}

		private async Task FlushAsync(List<OperationLogEntry> entries, CancellationToken ct)
		{
			using var scope = _scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

			foreach (var entry in entries)
			{
				db.OperationLogs.Add(new OperationLog
				{
					CreatedAtUtc = entry.OccurredOnUtc.UtcDateTime,
					ServiceName = entry.ServiceName,
					TraceId = entry.TraceId,
					SpanId = entry.SpanId,
					UserId = entry.UserId,
					UserName = entry.UserName,
					OperationName = entry.OperationName,
					Path = entry.Path,
					Method = entry.Method,
					Ip = entry.Ip,
					StatusCode = entry.StatusCode,
					Success = entry.Success,
					DurationMs = entry.DurationMs,
					RequestJson = entry.RequestJson,
					ResponseJson = entry.ResponseJson,
					ExceptionType = entry.ExceptionType,
					ExceptionMessage = entry.ExceptionMessage,
					ExceptionStackTrace = entry.ExceptionStackTrace
				});
			}

			await db.SaveChangesAsync(ct).ConfigureAwait(false);
		}
	}
}
