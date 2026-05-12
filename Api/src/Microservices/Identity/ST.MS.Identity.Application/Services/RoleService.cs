using Microsoft.EntityFrameworkCore;
using ST.MS.Identity.Application.Dtos.Role;
using ST.MS.Identity.Application.IServices;
using ST.MS.Identity.Domain.Aggregates.RoleAggregate;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Application.Dtos;
using ST.Shared.Application.Services;
using ST.Shared.Exceptions;

namespace ST.MS.Identity.Application.Services;

public sealed class RoleService : AbstractAppService, IRoleService
{
	private readonly IdentityDbContext _dbContext;

	public RoleService(IdentityDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<PagedResultDto<RoleListItemDto>> GetPageAsync(RoleQueryInputDto input)
	{
		var (pageIndex, pageSize, skip) = input.Normalize();

		var query = _dbContext.Role
			.AsNoTracking()
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(input.Keyword))
		{
			var keyword = input.Keyword.Trim();
			query = query.Where(x =>
				x.Code.Contains(keyword) ||
				x.Name.Contains(keyword) ||
				x.Description.Contains(keyword));
		}

		if (input.IsSystem.HasValue)
		{
			query = query.Where(x => x.IsSystem == input.IsSystem.Value);
		}

		if (input.IsDefault.HasValue)
		{
			query = query.Where(x => x.IsDefault == input.IsDefault.Value);
		}

		var totalCount = await query.LongCountAsync();
		var items = await query
			.OrderByDescending(x => x.CreateTime)
			.Skip(skip)
			.Take(pageSize)
			.Select(x => new RoleListItemDto
			{
				Id = x.Id,
				Code = x.Code,
				Name = x.Name,
				Description = x.Description,
				IsSystem = x.IsSystem,
				IsDefault = x.IsDefault,
				UserCount = _dbContext.Users.Count(u => u.UserRoles.Any(ur => ur.RoleId == x.Id)),
				PermissionCount = x.RolePermissions.Count,
				CreateTime = x.CreateTime,
				ModifyTime = x.ModifyTime
			})
			.ToListAsync();

		return new PagedResultDto<RoleListItemDto>
		{
			PageIndex = pageIndex,
			PageSize = pageSize,
			TotalCount = totalCount,
			Items = items
		};
	}

	public async Task<RoleDetailDto> GetDetailAsync(Guid id)
	{
		var role = await LoadRoleAsync(id);
		return new RoleDetailDto
		{
			Id = role.Id,
			Code = role.Code,
			Name = role.Name,
			Description = role.Description,
			IsSystem = role.IsSystem,
			IsDefault = role.IsDefault,
			CreateTime = role.CreateTime,
			ModifyTime = role.ModifyTime,
			PermissionIds = role.RolePermissions
				.Select(x => x.PermissionId)
				.Distinct()
				.ToList()
		};
	}

	public async Task<Guid> CreateAsync(CreateRoleInputDto input)
	{
		var code = NormalizeRequired(input.Code, "角色编码不能为空");
		var name = NormalizeRequired(input.Name, "角色名称不能为空");
		var description = (input.Description ?? string.Empty).Trim();

		await ValidateRoleCodeAsync(code, null);
		var permissionIds = await ValidatePermissionIdsAsync(input.PermissionIds);

		var role = new Role(name, code, description, input.IsSystem, input.IsDefault)
		{
			Id = Guid.CreateVersion7()
		};

		ApplyRolePermissions(role, permissionIds);

		_dbContext.Role.Add(role);
		await _dbContext.SaveChangesAsync();
		return role.Id;
	}

	public async Task UpdateAsync(Guid id, UpdateRoleInputDto input)
	{
		var role = await LoadRoleAsync(id);
		var code = NormalizeRequired(input.Code, "角色编码不能为空");
		var name = NormalizeRequired(input.Name, "角色名称不能为空");
		var description = (input.Description ?? string.Empty).Trim();

		await ValidateRoleCodeAsync(code, id);
		var permissionIds = await ValidatePermissionIdsAsync(input.PermissionIds);

		role.SetCode(code);
		role.SetName(name);
		role.SetDescription(description);
		role.SetIsSystem(input.IsSystem);
		role.SetIsDefault(input.IsDefault);

		ApplyRolePermissions(role, permissionIds);
		await _dbContext.SaveChangesAsync();
	}

	public async Task ChangePermissionsAsync(Guid id, ChangeRolePermissionsInputDto input)
	{
		var role = await LoadRoleAsync(id);
		var permissionIds = await ValidatePermissionIdsAsync(input.PermissionIds);

		ApplyRolePermissions(role, permissionIds);
		await _dbContext.SaveChangesAsync();
	}

	public async Task DeleteAsync(Guid id)
	{
		var role = await LoadRoleAsync(id);

		if (role.IsSystem)
		{
			throw new BusinessException("系统角色不允许删除");
		}

		var hasUsers = await _dbContext.Users.AnyAsync(x => x.UserRoles.Any(ur => ur.RoleId == id));
		if (hasUsers)
		{
			throw new BusinessException("角色已分配给用户，不能删除");
		}

		role.RolePermissions.Clear();
		role.IsDeleted = true;
		await _dbContext.SaveChangesAsync();
	}

	private async Task<Role> LoadRoleAsync(Guid id)
	{
		return await _dbContext.Role
			.Include(x => x.RolePermissions)
			.FirstOrDefaultAsync(x => x.Id == id)
			?? throw new BusinessException("角色不存在");
	}

	private async Task ValidateRoleCodeAsync(string code, Guid? excludeId)
	{
		var exists = await _dbContext.Role.AnyAsync(x => x.Code == code && x.Id != excludeId);
		if (exists)
		{
			throw new BusinessException("角色编码已存在");
		}
	}

	private async Task<HashSet<Guid>> ValidatePermissionIdsAsync(IEnumerable<Guid>? permissionIds)
	{
		var ids = permissionIds?
			.Where(x => x != Guid.Empty)
			.Distinct()
			.ToHashSet() ?? [];

		if (ids.Count == 0)
		{
			return ids;
		}

		var existingIds = await _dbContext.Permissions
			.AsNoTracking()
			.Where(x => ids.Contains(x.Id))
			.Select(x => x.Id)
			.ToListAsync();

		if (existingIds.Count != ids.Count)
		{
			throw new BusinessException("存在无效的菜单权限");
		}

		return ids;
	}

	private static void ApplyRolePermissions(Role role, IReadOnlySet<Guid> permissionIds)
	{
		role.RolePermissions.RemoveAll(x => !permissionIds.Contains(x.PermissionId));

		foreach (var permissionId in permissionIds)
		{
			if (role.RolePermissions.Any(x => x.PermissionId == permissionId))
			{
				continue;
			}

			role.RolePermissions.Add(new RolePermission
			{
				RoleId = role.Id,
				PermissionId = permissionId
			});
		}
	}

	private static string NormalizeRequired(string? value, string message)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new BusinessException(message);
		}

		return value.Trim();
	}
}
