using Microsoft.AspNetCore.Mvc;
using ST.MS.Identity.Application.Dtos.Role;
using ST.MS.Identity.Application.IServices;
using ST.Shared.Attributes;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Identity.Api.Controllers;

[Route("api/roles")]
public sealed class RoleController : AbstractControllerBase
{
	private readonly IRoleService _roleService;

	public RoleController(IRoleService roleService)
	{
		_roleService = roleService;
	}

	[HttpGet]
	public async Task<IActionResult> GetPage([FromQuery] RoleQueryInputDto input)
	{
		var result = await _roleService.GetPageAsync(input);
		return Ok(result);
	}

	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetDetail(Guid id)
	{
		var result = await _roleService.GetDetailAsync(id);
		return Ok(result);
	}

	[HttpPost]
	[OperationLog("新增角色", RecordRequest = true, RecordResponse = false)]
	public async Task<IActionResult> Create(CreateRoleInputDto input)
	{
		var id = await _roleService.CreateAsync(input);
		return Ok(new { Id = id });
	}

	[HttpPut("{id:guid}")]
	[OperationLog("编辑角色", RecordRequest = true, RecordResponse = false)]
	public async Task Update(Guid id, UpdateRoleInputDto input)
	{
		await _roleService.UpdateAsync(id, input);
	}

	[HttpPut("{id:guid}/permissions")]
	[OperationLog("分配角色菜单权限", RecordRequest = true, RecordResponse = false)]
	public async Task ChangePermissions(Guid id, ChangeRolePermissionsInputDto input)
	{
		await _roleService.ChangePermissionsAsync(id, input);
	}

	[HttpDelete("{id:guid}")]
	[OperationLog("删除角色", RecordRequest = true, RecordResponse = false)]
	public async Task Delete(Guid id)
	{
		await _roleService.DeleteAsync(id);
	}
}
