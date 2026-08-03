using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Test.Application.Services;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Test.Api.Controllers;

/// <summary>
/// 可靠消息基础设施验证接口。
/// 用于测试 Outbox / Inbox 表的读写、状态变更和幂等功能。
/// </summary>
[AllowAnonymous]
[Route("api/reliable-messaging")]
public class ReliableMessagingTestController : AbstractControllerBase
{
	private readonly ReliableMessagingTestService _service;
	private readonly ILogger<ReliableMessagingTestController> _logger;

	public ReliableMessagingTestController(
		ReliableMessagingTestService service,
		ILogger<ReliableMessagingTestController> logger)
	{
		_service = service;
		_logger = logger;
	}

	// ==================== Outbox 接口 ====================

	/// <summary>
	/// 写入一条 Outbox 消息
	/// </summary>
	[HttpPost("outbox")]
	public async Task<ActionResult<OutboxMessage>> AddOutboxMessage(
		[FromQuery] Guid? aggregateId = null,
		[FromQuery] string? eventType = null,
		[FromBody] object? message = null)
	{
		var payload = message ?? new { Text = "Hello Outbox", Timestamp = DateTime.UtcNow };
		var result = await _service.AddOutboxMessageAsync(
			aggregateId ?? Guid.CreateVersion7(),
			eventType ?? "TestIntegrationEvent",
			payload);

		_logger.LogInformation("Outbox 消息已写入: {Id}", result.Id);
		return Ok(result);
	}

	/// <summary>
	/// 查询所有 Outbox 消息
	/// </summary>
	[HttpGet("outbox")]
	public async Task<ActionResult<List<OutboxMessage>>> GetAllOutboxMessages()
	{
		return Ok(await _service.GetAllOutboxMessagesAsync());
	}

	/// <summary>
	/// 查询待发送的 Outbox 消息
	/// </summary>
	[HttpGet("outbox/pending")]
	public async Task<ActionResult<List<OutboxMessage>>> GetPendingOutboxMessages()
	{
		return Ok(await _service.GetPendingOutboxMessagesAsync());
	}

	/// <summary>
	/// 标记 Outbox 消息为已发送
	/// </summary>
	[HttpPut("outbox/{messageId}/sent")]
	public async Task<IActionResult> MarkOutboxAsSent(Guid messageId)
	{
		var success = await _service.MarkOutboxAsSentAsync(messageId);
		if (!success) return NotFound(new { Error = "消息不存在" });

		_logger.LogInformation("Outbox 消息已标记为发送: {Id}", messageId);
		return Ok(new { Message = "已标记为 Sent", Id = messageId });
	}

	/// <summary>
	/// 标记 Outbox 消息为失败
	/// </summary>
	[HttpPut("outbox/{messageId}/failed")]
	public async Task<IActionResult> MarkOutboxAsFailed(Guid messageId, [FromQuery] string? error = null)
	{
		var success = await _service.MarkOutboxAsFailedAsync(messageId, error ?? "模拟发送失败");
		if (!success) return NotFound(new { Error = "消息不存在" });

		_logger.LogWarning("Outbox 消息已标记为失败: {Id}", messageId);
		return Ok(new { Message = "已标记为 Failed", Id = messageId });
	}

	// ==================== Inbox 接口 ====================

	/// <summary>
	/// 写入一条 Inbox 消息（幂等）
	/// </summary>
	[HttpPost("inbox")]
	public async Task<ActionResult<InboxMessage>> AddInboxMessage(
		[FromQuery] Guid? messageId = null,
		[FromQuery] string? consumer = null,
		[FromQuery] string? eventType = null)
	{
		try
		{
			var result = await _service.AddInboxMessageAsync(
				messageId ?? Guid.CreateVersion7(),
				consumer ?? "TestConsumer",
				eventType ?? "TestIntegrationEvent");

			_logger.LogInformation("Inbox 消息已写入: {Id}", result.Id);
			return Ok(result);
		}
		catch (BusinessException ex)
		{
			return Conflict(new { Error = ex.Message });
		}
	}

	/// <summary>
	/// 查询所有 Inbox 消息
	/// </summary>
	[HttpGet("inbox")]
	public async Task<ActionResult<List<InboxMessage>>> GetAllInboxMessages()
	{
		return Ok(await _service.GetAllInboxMessagesAsync());
	}

	/// <summary>
	/// 检查 Inbox 消息是否存在（幂等检查）
	/// </summary>
	[HttpGet("inbox/exists")]
	public async Task<ActionResult<object>> CheckInboxExists(
		[FromQuery] Guid messageId,
		[FromQuery] string consumer = "TestConsumer")
	{
		var exists = await _service.CheckInboxExistsAsync(messageId, consumer);
		return Ok(new { MessageId = messageId, Consumer = consumer, Exists = exists });
	}

	/// <summary>
	/// 标记 Inbox 消息为已处理
	/// </summary>
	[HttpPut("inbox/processed")]
	public async Task<IActionResult> MarkInboxAsProcessed(
		[FromQuery] Guid messageId,
		[FromQuery] string consumer = "TestConsumer")
	{
		var success = await _service.MarkInboxAsProcessedAsync(messageId, consumer);
		if (!success) return NotFound(new { Error = "消息不存在" });

		return Ok(new { Message = "已标记为 Processed", MessageId = messageId, Consumer = consumer });
	}

	// ==================== 综合测试接口 ====================

	/// <summary>
	/// 完整流程测试：写入 Outbox → 查询待发送 → 标记已发送 → 写入 Inbox → 检查幂等
	/// </summary>
	[HttpPost("full-flow")]
	public async Task<ActionResult<object>> TestFullFlow()
	{
		var result = new Dictionary<string, object>();
		var aggregateId = Guid.CreateVersion7();
		var consumer = "FullFlowConsumer";

		// 1. 写入 Outbox
		var outboxMsg = await _service.AddOutboxMessageAsync(
			aggregateId,
			"OrderCreatedIntegrationEvent",
			new { OrderId = aggregateId, Amount = 99.99m });
		result["Step1_OutboxCreated"] = new { outboxMsg.Id, outboxMsg.Status };

		// 2. 查询待发送
		var pending = await _service.GetPendingOutboxMessagesAsync();
		result["Step2_PendingCount"] = pending.Count;

		// 3. 标记已发送
		await _service.MarkOutboxAsSentAsync(outboxMsg.Id);
		result["Step3_MarkedAsSent"] = outboxMsg.Id;

		// 4. 写入 Inbox
		var inboxMsg = await _service.AddInboxMessageAsync(
			outboxMsg.Id, consumer, outboxMsg.EventType);
		result["Step4_InboxCreated"] = new { inboxMsg.Id, inboxMsg.MessageId };

		// 5. 幂等检查
		var exists = await _service.CheckInboxExistsAsync(outboxMsg.Id, consumer);
		result["Step5_IdempotentCheck"] = new { Exists = exists };

		// 6. 标记已处理
		await _service.MarkInboxAsProcessedAsync(outboxMsg.Id, consumer);
		result["Step6_MarkedAsProcessed"] = outboxMsg.Id;

		_logger.LogInformation("完整流程测试完成: {AggregateId}", aggregateId);
		return Ok(result);
	}

	/// <summary>
	/// 清理所有测试数据
	/// </summary>
	[HttpDelete("clear")]
	public async Task<IActionResult> ClearAll()
	{
		var count = await _service.ClearAllAsync();
		_logger.LogInformation("已清理 {Count} 条测试数据", count);
		return Ok(new { DeletedCount = count });
	}
}
