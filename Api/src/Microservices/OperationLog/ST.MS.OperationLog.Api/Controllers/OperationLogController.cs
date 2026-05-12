using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.MS.OperationLog.Application.Dtos.OperationLog;
using ST.MS.OperationLog.Application.IServices;
using ST.Shared.WebApi.Controller;

namespace ST.MS.OperationLog.Api.Controllers;

[Authorize(Policy = "perm:system:operationlog:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/operation-logs")]
public sealed class OperationLogController : AbstractControllerBase
{
	private readonly IOperationLogQueryService _operationLogQueryService;

	public OperationLogController(IOperationLogQueryService operationLogQueryService)
	{
		_operationLogQueryService = operationLogQueryService;
	}

	[HttpGet]
	public async Task<IActionResult> GetPage([FromQuery] OperationLogQueryInputDto input)
	{
		var result = await _operationLogQueryService.GetPageAsync(input);
		return Ok(result);
	}

	[HttpGet("{id:long}")]
	public async Task<IActionResult> GetDetail(long id)
	{
		var result = await _operationLogQueryService.GetDetailAsync(id);
		return Ok(result);
	}
}
