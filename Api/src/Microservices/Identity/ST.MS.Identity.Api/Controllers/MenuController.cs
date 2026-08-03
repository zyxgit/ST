using ST.MS.Identity.Application.Dtos.Menu;
using ST.MS.Identity.Application.IServices;

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
	[PermissionAuthorize(Permission.MenuQuery)]
	public async Task<ActionResult<MenuTreeNodeDto>> GetTree()
	{
		var result = await _menuService.GetTreeAsync();
		return Ok(result);
	}

	[HttpGet("my-tree")]
	public async Task<ActionResult<IReadOnlyList<MenuTreeNodeDto>>> GetCurrentUserTree()
	{
		var result = await _menuService.GetCurrentUserTreeAsync();
		return Ok(result);
	}

	[HttpGet("{id:guid}")]
	[PermissionAuthorize(Permission.MenuQuery)]
	public async Task<ActionResult<MenuDetailDto>> GetDetail(Guid id)
	{
		var result = await _menuService.GetDetailAsync(id);
		return Ok(result);
	}

	[HttpPost]
	[PermissionAuthorize(Permission.MenuCreate)]
	[OperationLog("新增菜单", RecordRequest = true, RecordResponse = false)]
	public async Task<ActionResult<Guid>> Create(CreateMenuInputDto input)
	{
		var id = await _menuService.CreateAsync(input);
		return Ok(id);
	}

	[HttpPut("{id:guid}")]
	[PermissionAuthorize(Permission.MenuUpdate)]
	[OperationLog("编辑菜单", RecordRequest = true, RecordResponse = false)]
	public async Task Update(Guid id, UpdateMenuInputDto input)
	{
		await _menuService.UpdateAsync(id, input);
	}

	[HttpDelete("{id:guid}")]
	[PermissionAuthorize(Permission.MenuDelete)]
	[OperationLog("删除菜单", RecordRequest = true, RecordResponse = false)]
	public async Task Delete(Guid id)
	{
		await _menuService.DeleteAsync(id);
	}
}
