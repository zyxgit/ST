using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.MS.Identity.Application.Dtos.Menu;
using ST.MS.Identity.Application.IServices;
using ST.Shared.Attributes;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Identity.Api.Controllers;

[Route("api/menus")]
public sealed class MenuController : AbstractControllerBase
{
	private readonly IMenuService _menuService;

	public MenuController(IMenuService menuService)
	{
		_menuService = menuService;
	}

	[HttpGet("tree")]
	[Authorize(Policy = "perm:system:menu:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<IActionResult> GetTree()
	{
		var result = await _menuService.GetTreeAsync();
		return Ok(result);
	}

	[HttpGet("my-tree")]
	public async Task<IActionResult> GetCurrentUserTree()
	{
		var result = await _menuService.GetCurrentUserTreeAsync();
		return Ok(result);
	}

	[HttpGet("{id:guid}")]
	[Authorize(Policy = "perm:system:menu:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<IActionResult> GetDetail(Guid id)
	{
		var result = await _menuService.GetDetailAsync(id);
		return Ok(result);
	}

	[HttpPost]
	[Authorize(Policy = "perm:system:menu:create", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[OperationLog("新增菜单", RecordRequest = true, RecordResponse = false)]
	public async Task<IActionResult> Create(CreateMenuInputDto input)
	{
		var id = await _menuService.CreateAsync(input);
		return Ok(new { Id = id });
	}

	[HttpPut("{id:guid}")]
	[Authorize(Policy = "perm:system:menu:update", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[OperationLog("编辑菜单", RecordRequest = true, RecordResponse = false)]
	public async Task Update(Guid id, UpdateMenuInputDto input)
	{
		await _menuService.UpdateAsync(id, input);
	}

	[HttpDelete("{id:guid}")]
	[Authorize(Policy = "perm:system:menu:delete", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	[OperationLog("删除菜单", RecordRequest = true, RecordResponse = false)]
	public async Task Delete(Guid id)
	{
		await _menuService.DeleteAsync(id);
	}
}
