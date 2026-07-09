using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ST.Infra.EventBus.OperationLog;
using ST.MS.OperationLog.Application.IServices;
using ST.MS.OperationLog.Infra.DbContext;

namespace ST.MS.OperationLog.Application.Services;

/// <summary>
/// 死信重放服务实现（API 进程使用）。
/// 从数据库读取死信消息并重新发布到 RabbitMQ。
/// </summary>
public sealed class DeadLetterReplayService : IDeadLetterService
{
	private readonly RabbitMqOperationLogOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<DeadLetterReplayService> _logger;

	public DeadLetterReplayService(
		RabbitMqOperationLogOptions options,
		IServiceScopeFactory scopeFactory,
		ILogger<DeadLetterReplayService> logger)
	{
		_options = options;
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	/// <inheritdoc />
	public async Task<bool> ReplayAsync(Guid id)
	{
		using var scope = _scopeFactory.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<OperationLogDbContext>();

		var deadLetter = await db.DeadLetterMessages.FindAsync(id);
		if (deadLetter is null)
		{
			_logger.LogWarning("Dead letter message not found: {Id}", id);
			return false;
		}

		IConnection? connection = null;
		IChannel? channel = null;

		try
		{
			var factory = new ConnectionFactory
			{
				HostName = _options.HostName,
				Port = _options.Port,
				UserName = _options.UserName,
				Password = _options.Password,
				VirtualHost = _options.VirtualHost
			};

			connection = await factory.CreateConnectionAsync();
			channel = await connection.CreateChannelAsync();

			var body = Encoding.UTF8.GetBytes(deadLetter.OriginalMessage);
			var properties = new BasicProperties
			{
				ContentType = "application/json",
				DeliveryMode = DeliveryModes.Persistent
			};

			await channel.BasicPublishAsync(
				deadLetter.ExchangeName,
				deadLetter.RoutingKey,
				false,
				properties,
				body);

			deadLetter.ReplayedAtUtc = DateTime.UtcNow;
			deadLetter.ReplayResult = "Success";
			await db.SaveChangesAsync();

			_logger.LogInformation("Dead letter message replayed: {Id}", id);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to replay dead letter message: {Id}", id);

			deadLetter.ReplayResult = $"Failed: {ex.Message}";
			await db.SaveChangesAsync();

			return false;
		}
		finally
		{
			try { channel?.Dispose(); } catch { }
			try { connection?.Dispose(); } catch { }
		}
	}

	/// <inheritdoc />
	public async Task<(int Replayed, int Failed)> BatchReplayAsync(List<Guid> ids)
	{
		var replayed = 0;
		var failed = 0;

		foreach (var id in ids)
		{
			var success = await ReplayAsync(id);
			if (success)
				replayed++;
			else
				failed++;
		}

		return (replayed, failed);
	}
}
