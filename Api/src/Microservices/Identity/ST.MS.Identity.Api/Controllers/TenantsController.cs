using Microsoft.AspNetCore.Mvc;
using ST.MS.Identity.Application.Dtos.Tenant;
using ST.MS.Identity.Application.IServices;
using ST.Shared.Attributes;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Identity.Api.Controllers;

[Route("api/tenants")]
public sealed class TenantsController : AbstractControllerBase
{
	private readonly ITenantService _tenantService;

	public TenantsController(ITenantService tenantService)
	{
		_tenantService = tenantService;
	}

	/// <summary>
	/// 分页查询租户
	/// </summary>
	[HttpGet]
	public async Task<IActionResult> GetPage([FromQuery] TenantQueryInputDto input)
	{
		var result = await _tenantService.GetPageAsync(input);
		return Ok(result);
	}

	/// <summary>
	/// 租户详情
	/// </summary>
	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetDetail(Guid id)
	{
		var result = await _tenantService.GetDetailAsync(id);
		return Ok(result);
	}

	/// <summary>
	/// 创建租户
	/// </summary>
	[HttpPost]
	[OperationLog("新增租户", RecordRequest = true, RecordResponse = false)]
	public async Task<IActionResult> Create(CreateTenantInputDto input)
	{
		var id = await _tenantService.CreateAsync(input);
		return Ok(new { Id = id });
	}

	/// <summary>
	/// 更新租户信息
	/// </summary>
	[HttpPut("{id:guid}")]
	[OperationLog("编辑租户", RecordRequest = true, RecordResponse = false)]
	public async Task Update(Guid id, UpdateTenantInputDto input)
	{
		await _tenantService.UpdateAsync(id, input);
	}

	/// <summary>
	/// 激活租户
	/// </summary>
	[HttpPost("{id:guid}/activate")]
	[OperationLog("激活租户")]
	public async Task Activate(Guid id)
	{
		await _tenantService.ActivateAsync(id);
	}

	/// <summary>
	/// 暂停租户
	/// </summary>
	[HttpPost("{id:guid}/suspend")]
	[OperationLog("暂停租户")]
	public async Task Suspend(Guid id)
	{
		await _tenantService.SuspendAsync(id);
	}

	/// <summary>
	/// 删除租户
	/// </summary>
	[HttpDelete("{id:guid}")]
	[OperationLog("删除租户")]
	public async Task Delete(Guid id)
	{
		await _tenantService.DeleteAsync(id);
	}

	/// <summary>
	/// 添加租户用户
	/// </summary>
	[HttpPost("{tenantId:guid}/users")]
	[OperationLog("添加租户用户", RecordRequest = true)]
	public async Task AddUser(Guid tenantId, AddTenantUserInputDto input)
	{
		await _tenantService.AddUserAsync(tenantId, input);
	}

	/// <summary>
	/// 移除租户用户
	/// </summary>
	[HttpDelete("{tenantId:guid}/users/{userId:guid}")]
	[OperationLog("移除租户用户")]
	public async Task RemoveUser(Guid tenantId, Guid userId)
	{
		await _tenantService.RemoveUserAsync(tenantId, userId);
	}

	/// <summary>
	/// 查询租户用户列表
	/// </summary>
	[HttpGet("{tenantId:guid}/users")]
	public async Task<IActionResult> GetUsers(Guid tenantId)
	{
		var result = await _tenantService.GetUsersAsync(tenantId);
		return Ok(result);
	}

	/// <summary>
	/// 查询租户配额
	/// </summary>
	[HttpGet("{tenantId:guid}/quota")]
	public async Task<IActionResult> GetQuota(Guid tenantId)
	{
		var result = await _tenantService.GetQuotaAsync(tenantId);
		return Ok(result);
	}

	/// <summary>
	/// 更新租户配额
	/// </summary>
	[HttpPut("{tenantId:guid}/quota")]
	[OperationLog("更新租户配额", RecordRequest = true)]
	public async Task UpdateQuota(Guid tenantId, UpdateTenantQuotaInputDto input)
	{
		await _tenantService.UpdateQuotaAsync(tenantId, input);
	}
}
