using Microsoft.EntityFrameworkCore;
using ST.MS.Identity.Application.Dtos.Menu;
using ST.MS.Identity.Application.IServices;
using ST.MS.Identity.Domain.Aggregates.PermissionAggregate;
using ST.MS.Identity.Domain.Aggregates.RoleAggregate;
using ST.MS.Identity.Domain.Enums;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Application.Services;
using ST.Shared.Security;

namespace ST.MS.Identity.Application.Services;

public sealed class MenuService : AbstractAppService, IMenuService
{
	private readonly IdentityDbContext _dbContext;
	private readonly IUserContext _userContext;

	public MenuService(IdentityDbContext dbContext, IUserContext userContext)
	{
		_dbContext = dbContext;
		_userContext = userContext;
	}

	public async Task<IReadOnlyList<MenuTreeNodeDto>> GetTreeAsync()
	{
		var nodes = await LoadAllNodesAsync();
		return BuildTree(nodes.Select(CloneNodeWithoutChildren).ToList());
	}

	public async Task<IReadOnlyList<MenuTreeNodeDto>> GetCurrentUserTreeAsync()
	{
		var nodes = await LoadAllNodesAsync();
		var grantedCodes = _userContext.Permissions
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		if (!_userContext.IsAuthenticated || grantedCodes.Count == 0)
		{
			return [];
		}

		var visibleNodes = FilterVisibleNodes(nodes, grantedCodes);
		return BuildTree(visibleNodes);
	}

	public async Task<MenuDetailDto> GetDetailAsync(Guid id)
	{
		return await _dbContext.Permissions
			.AsNoTracking()
			.Where(x => x.Id == id)
			.Select(x => new MenuDetailDto
			{
				Id = x.Id,
				ParentId = x.PId,
				Code = x.Code,
				Name = x.Name,
				Type = x.Type,
				Path = x.Path,
				MenuIcon = x.MenuIcon,
				Component = x.Component,
				IsLink = x.IsLink,
				KeepAlive = x.KeepAlive,
				IsHide = x.IsHide,
				CreateTime = x.CreateTime,
				ModifyTime = x.ModifyTime
			})
			.FirstOrDefaultAsync()
			?? throw new BusinessException("菜单不存在");
	}

	public async Task<Guid> CreateAsync(CreateMenuInputDto input)
	{
		var code = NormalizeRequired(input.Code, "菜单编码不能为空");
		var name = NormalizeRequired(input.Name, "菜单名称不能为空");
		await ValidateCodeAsync(code, null);
		await ValidateParentAsync(input.ParentId, null);

		var permission = new Permission(input.ParentId, code, name, input.Type, NormalizePath(input.Type, input.Path))
		{
			Id = Guid.CreateVersion7()
		};

		permission.UpdatePresentation(input.MenuIcon, input.Component, input.IsLink, input.KeepAlive, input.IsHide);

		_dbContext.Permissions.Add(permission);
		await _dbContext.SaveChangesAsync();
		return permission.Id;
	}

	public async Task UpdateAsync(Guid id, UpdateMenuInputDto input)
	{
		var permission = await _dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("菜单不存在");

		var code = NormalizeRequired(input.Code, "菜单编码不能为空");
		var name = NormalizeRequired(input.Name, "菜单名称不能为空");
		await ValidateCodeAsync(code, id);
		await ValidateParentAsync(input.ParentId, id);

		permission.UpdateBasicInfo(input.ParentId, code, name, input.Type, NormalizePath(input.Type, input.Path));
		permission.UpdatePresentation(input.MenuIcon, input.Component, input.IsLink, input.KeepAlive, input.IsHide);

		await _dbContext.SaveChangesAsync();
	}

	public async Task DeleteAsync(Guid id)
	{
		var permission = await _dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("菜单不存在");

		var hasChildren = await _dbContext.Permissions.AnyAsync(x => x.PId == id);
		if (hasChildren)
		{
			throw new BusinessException("请先删除子菜单");
		}

		var rolePermissions = await _dbContext.Set<RolePermission>()
			.Where(x => x.PermissionId == id)
			.ToListAsync();

		_dbContext.RemoveRange(rolePermissions);
		permission.SoftDelete();
		await _dbContext.SaveChangesAsync();
	}

	private async Task ValidateCodeAsync(string code, Guid? excludeId)
	{
		var exists = await _dbContext.Permissions.AnyAsync(x => x.Code == code && x.Id != excludeId);
		if (exists)
		{
			throw new BusinessException("菜单编码已存在");
		}
	}

	private async Task ValidateParentAsync(Guid? parentId, Guid? currentId)
	{
		if (!parentId.HasValue)
		{
			return;
		}

		if (parentId == currentId)
		{
			throw new BusinessException("不能设置自己为父级");
		}

		var parent = await _dbContext.Permissions
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == parentId.Value)
			?? throw new BusinessException("父级菜单不存在");

		if (parent.Type == PermissionType.Button)
		{
			throw new BusinessException("按钮类型不能作为父级菜单");
		}
	}

	private static string? NormalizePath(PermissionType type, string? path)
	{
		var normalizedPath = string.IsNullOrWhiteSpace(path) ? null : path.Trim();

		if (type == PermissionType.Button)
		{
			if (!string.IsNullOrWhiteSpace(normalizedPath))
			{
				throw new BusinessException("按钮类型不能设置路由");
			}

			return null;
		}

		if (string.IsNullOrWhiteSpace(normalizedPath))
		{
			throw new BusinessException("目录或菜单必须设置路由");
		}

		return normalizedPath;
	}

	private static string NormalizeRequired(string? value, string message)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new BusinessException(message);
		}

		return value.Trim();
	}

	private async Task<List<MenuTreeNodeDto>> LoadAllNodesAsync()
	{
		return await _dbContext.Permissions
			.AsNoTracking()
			.OrderBy(x => x.Type)
			.ThenBy(x => x.CreateTime)
			.Select(x => new MenuTreeNodeDto
			{
				Id = x.Id,
				ParentId = x.PId,
				Code = x.Code,
				Name = x.Name,
				Type = x.Type,
				Path = x.Path,
				MenuIcon = x.MenuIcon,
				Component = x.Component,
				IsLink = x.IsLink,
				KeepAlive = x.KeepAlive,
				IsHide = x.IsHide
			})
			.ToListAsync();
	}

	private static List<MenuTreeNodeDto> FilterVisibleNodes(
		IReadOnlyList<MenuTreeNodeDto> nodes,
		IReadOnlySet<string> grantedCodes)
	{
		var lookup = nodes.ToDictionary(x => x.Id);
		var visibleIds = new HashSet<Guid>();

		foreach (var node in nodes.Where(x => grantedCodes.Contains(x.Code)))
		{
			var current = node;

			while (visibleIds.Add(current.Id) && current.ParentId.HasValue && lookup.TryGetValue(current.ParentId.Value, out var parent))
			{
				current = parent;
			}
		}

		return nodes
			.Where(x => visibleIds.Contains(x.Id))
			.Select(CloneNodeWithoutChildren)
			.ToList();
	}

	private static IReadOnlyList<MenuTreeNodeDto> BuildTree(IReadOnlyList<MenuTreeNodeDto> nodes)
	{
		var lookup = nodes.ToDictionary(x => x.Id);
		var roots = new List<MenuTreeNodeDto>();

		foreach (var node in nodes)
		{
			if (node.ParentId.HasValue && lookup.TryGetValue(node.ParentId.Value, out var parent))
			{
				parent.Children.Add(node);
			}
			else
			{
				roots.Add(node);
			}
		}

		return roots;
	}

	private static MenuTreeNodeDto CloneNodeWithoutChildren(MenuTreeNodeDto node)
	{
		return new MenuTreeNodeDto
		{
			Id = node.Id,
			ParentId = node.ParentId,
			Code = node.Code,
			Name = node.Name,
			Type = node.Type,
			Path = node.Path,
			MenuIcon = node.MenuIcon,
			Component = node.Component,
			IsLink = node.IsLink,
			KeepAlive = node.KeepAlive,
			IsHide = node.IsHide
		};
	}
}
