using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using ST.Infra.EventBus.OperationLog;
using ST.MS.OperationLog.Application.IServices;
using ST.MS.OperationLog.Infra.DbContext;
using ST.MS.OperationLog.Infra.Entities;

namespace ST.MS.OperationLog.Consumer;

/// <summary>
/// 死信服务实现。
/// 负责将失败消息写入数据库，支持查询和重放。
/// </summary>
public sealed class DeadLetterService : IDeadLetterService
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	private readonly RabbitMqOperationLogOptions _options;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<DeadLetterService> _logger;

	public DeadLetterService(
		RabbitMqOperationLogOptions options,
		IServiceScopeFactory scopeFactory,
		ILogger<DeadLetterService> logger)
	{
		_options = options;
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	/// <summary>
	/// 将失败消息保存到死信表。
	/// </summary>
	public async Task SendToDeadLetterAsync(
		string originalMessage,
		string queueName,
		string exchangeName,
		string routingKey,
		string errorMessage,
		string? errorStackTrace,
		int retryCount,
		int maxRetryCount,
		DateTime? messageCreatedAtUtc = null)
	{
		try
		{
			using var scope = _scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<OperationLogDbContext>();

			var deadLetter = new DeadLetterMessage
			{
				OriginalMessage = originalMessage,
				QueueName = queueName,
				ExchangeName = exchangeName,
				RoutingKey = routingKey,
				ErrorMessage = errorMessage,
				ErrorStackTrace = errorStackTrace,
				RetryCount = retryCount,
				MaxRetryCount = maxRetryCount,
				MessageCreatedAtUtc = messageCreatedAtUtc
			};

			db.DeadLetterMessages.Add(deadLetter);
			await db.SaveChangesAsync();

			_logger.LogInformation(
				"Message sent to dead letter. Queue={Queue} RetryCount={RetryCount} Error={Error}",
				queueName, retryCount, errorMessage);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to save dead letter message.");
		}
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
			// 创建 RabbitMQ 连接
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

			// 重新发布到原始队列
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

			// 更新重放状态
			deadLetter.ReplayedAtUtc = DateTime.UtcNow;
			deadLetter.ReplayResult = "Success";
			await db.SaveChangesAsync();

			_logger.LogInformation("Dead letter message replayed: {Id}", id);
			OperationLogMetrics.DeadLetterReplaySuccess.Add(1);
			return true;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to replay dead letter message: {Id}", id);

			deadLetter.ReplayResult = $"Failed: {ex.Message}";
			await db.SaveChangesAsync();

			OperationLogMetrics.DeadLetterReplayFailed.Add(1);
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
