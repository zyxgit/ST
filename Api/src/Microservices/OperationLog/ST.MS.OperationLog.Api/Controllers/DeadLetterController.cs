using Microsoft.AspNetCore.Mvc;
using ST.MS.OperationLog.Application.Dtos.DeadLetter;
using ST.MS.OperationLog.Application.IServices;
using ST.Shared.Attributes;
using ST.Shared.WebApi.Controller;

namespace ST.MS.OperationLog.Api.Controllers;

/// <summary>
/// 死信消息管理
/// </summary>
[Route("api/dead-letters")]
public sealed class DeadLetterController : AbstractControllerBase
{
	private readonly IDeadLetterQueryService _queryService;
	private readonly IDeadLetterService _deadLetterService;

	public DeadLetterController(
		IDeadLetterQueryService queryService,
		IDeadLetterService deadLetterService)
	{
		_queryService = queryService;
		_deadLetterService = deadLetterService;
	}

	/// <summary>
	/// 查询死信消息（分页）
	/// </summary>
	/// <param name="input">查询条件</param>
	[HttpGet]
	public async Task<IActionResult> Query([FromQuery] DeadLetterQueryInputDto input)
	{
		var result = await _queryService.QueryAsync(input);
		return Ok(result);
	}

	/// <summary>
	/// 获取死信消息详情
	/// </summary>
	/// <param name="id">死信消息 ID</param>
	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetById(Guid id)
	{
		var result = await _queryService.GetByIdAsync(id);
		return Ok(result);
	}

	/// <summary>
	/// 重放单条死信消息
	/// </summary>
	/// <param name="id">死信消息 ID</param>
	[HttpPost("{id:guid}/replay")]
	[OperationLog("重放死信消息", RecordRequest = true, RecordResponse = true)]
	public async Task<IActionResult> Replay(Guid id)
	{
		var success = await _deadLetterService.ReplayAsync(id);
		return Ok(new { success });
	}

	/// <summary>
	/// 批量重放死信消息
	/// </summary>
	/// <param name="request">包含要重放的 ID 列表</param>
	[HttpPost("batch-replay")]
	[OperationLog("批量重放死信消息", RecordRequest = true, RecordResponse = true)]
	public async Task<IActionResult> BatchReplay([FromBody] BatchReplayRequestDto request)
	{
		var (replayed, failed) = await _deadLetterService.BatchReplayAsync(request.Ids);
		return Ok(new BatchReplayResultDto { Replayed = replayed, Failed = failed });
	}
}
