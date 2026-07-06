using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.MS.Identity.Application.Dtos.Tenant;
using ST.MS.Identity.Application.IServices;
using ST.MS.Identity.Domain.Aggregates.TenantAggregate;
using ST.MS.Identity.Domain.Enums;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Application.Dtos;
using ST.Shared.Application.Services;
using ST.Shared.Exceptions;

namespace ST.MS.Identity.Application.Services;

public sealed class TenantService : AbstractAppService, ITenantService
{
	private readonly IdentityDbContext _dbContext;
	private readonly ILogger<TenantService> _logger;

	public TenantService(IdentityDbContext dbContext, ILogger<TenantService> logger)
	{
		_dbContext = dbContext;
		_logger = logger;
	}

	public async Task<PagedResultDto<TenantListItemDto>> GetPageAsync(TenantQueryInputDto input)
	{
		var (pageIndex, pageSize, skip) = input.Normalize();

		var query = _dbContext.Tenants
			.AsNoTracking()
			.Where(x => !x.IsDeleted)
			.AsQueryable();

		if (!string.IsNullOrWhiteSpace(input.Keyword))
		{
			var keyword = input.Keyword.Trim();
			query = query.Where(x =>
				x.Code.Contains(keyword) ||
				x.Name.Contains(keyword));
		}

		if (!string.IsNullOrWhiteSpace(input.Status) &&
			Enum.TryParse<TenantStatus>(input.Status, true, out var status))
		{
			query = query.Where(x => x.Status == status);
		}

		var totalCount = await query.LongCountAsync();
		var items = await query
			.OrderByDescending(x => x.CreateTime)
			.Skip(skip)
			.Take(pageSize)
			.Select(x => new TenantListItemDto
			{
				Id = x.Id,
				Code = x.Code,
				Name = x.Name,
				Status = x.Status.ToString(),
				PackageId = x.PackageId,
				ExpireAtUtc = x.ExpireAtUtc,
				UserCount = _dbContext.TenantUsers.Count(tu => tu.TenantId == x.Id),
				CreateTime = x.CreateTime
			})
			.ToListAsync();

		return new PagedResultDto<TenantListItemDto>
		{
			PageIndex = pageIndex,
			PageSize = pageSize,
			TotalCount = totalCount,
			Items = items
		};
	}

	public async Task<TenantDetailDto> GetDetailAsync(Guid id)
	{
		var tenant = await LoadTenantAsync(id);

		var userCount = await _dbContext.TenantUsers
			.AsNoTracking()
			.CountAsync(x => x.TenantId == id);

		var quota = await _dbContext.TenantQuotas
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.TenantId == id);

		return new TenantDetailDto
		{
			Id = tenant.Id,
			Code = tenant.Code,
			Name = tenant.Name,
			Status = tenant.Status.ToString(),
			PackageId = tenant.PackageId,
			ExpireAtUtc = tenant.ExpireAtUtc,
			UserCount = userCount,
			CreateTime = tenant.CreateTime,
			ModifyTime = tenant.ModifyTime,
			Quota = quota is not null ? new TenantQuotaDto
			{
				TenantId = quota.TenantId,
				MaxUsers = quota.MaxUsers,
				MaxStorageBytes = quota.MaxStorageBytes,
				MaxApiCallsPerDay = quota.MaxApiCallsPerDay,
				MaxFileSize = quota.MaxFileSize,
				MaxOrdersPerDay = quota.MaxOrdersPerDay
			} : null
		};
	}

	public async Task<Guid> CreateAsync(CreateTenantInputDto input)
	{
		var code = NormalizeRequired(input.Code, "租户编码不能为空");
		var name = NormalizeRequired(input.Name, "租户名称不能为空");

		await ValidateTenantCodeAsync(code, null);

		var tenant = new Tenant(code, name);

		_dbContext.Tenants.Add(tenant);

		// 创建默认配额
		var quota = new TenantQuota(tenant.Id);
		_dbContext.TenantQuotas.Add(quota);

		await _dbContext.SaveChangesAsync();
		_logger.LogInformation("Created tenant {TenantId} with code {Code}", tenant.Id, code);
		return tenant.Id;
	}

	public async Task UpdateAsync(Guid id, UpdateTenantInputDto input)
	{
		var tenant = await LoadTenantAsync(id);
		var name = NormalizeRequired(input.Name, "租户名称不能为空");

		tenant.UpdateInfo(name);
		tenant.SetPackage(input.PackageId);
		tenant.SetExpireDate(input.ExpireAtUtc);

		await _dbContext.SaveChangesAsync();
	}

	public async Task ActivateAsync(Guid id)
	{
		var tenant = await LoadTenantAsync(id);
		tenant.Activate();
		await _dbContext.SaveChangesAsync();
		_logger.LogInformation("Activated tenant {TenantId}", id);
	}

	public async Task SuspendAsync(Guid id)
	{
		var tenant = await LoadTenantAsync(id);
		tenant.Suspend();
		await _dbContext.SaveChangesAsync();
		_logger.LogInformation("Suspended tenant {TenantId}", id);
	}

	public async Task DeleteAsync(Guid id)
	{
		var tenant = await LoadTenantAsync(id);

		var hasUsers = await _dbContext.TenantUsers.AnyAsync(x => x.TenantId == id);
		if (hasUsers)
		{
			throw new BusinessException("租户下仍有用户，不能删除");
		}

		tenant.Delete();
		await _dbContext.SaveChangesAsync();
		_logger.LogInformation("Deleted tenant {TenantId}", id);
	}

	public async Task AddUserAsync(Guid tenantId, AddTenantUserInputDto input)
	{
		var tenant = await LoadTenantAsync(tenantId);

		if (tenant.Status != TenantStatus.Active)
		{
			throw new BusinessException("租户未激活，不能添加用户");
		}

		var user = await _dbContext.Users
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == input.UserId)
			?? throw new BusinessException("用户不存在");

		var exists = await _dbContext.TenantUsers
			.AnyAsync(x => x.TenantId == tenantId && x.UserId == input.UserId);

		if (exists)
		{
			throw new BusinessException("用户已在该租户中");
		}

		var tenantUser = new TenantUser
		{
			TenantId = tenantId,
			UserId = input.UserId,
			RoleInTenant = input.RoleInTenant,
			JoinedAtUtc = DateTime.UtcNow
		};

		_dbContext.TenantUsers.Add(tenantUser);
		await _dbContext.SaveChangesAsync();
		_logger.LogInformation("Added user {UserId} to tenant {TenantId}", input.UserId, tenantId);
	}

	public async Task RemoveUserAsync(Guid tenantId, Guid userId)
	{
		var tenantUser = await _dbContext.TenantUsers
			.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId)
			?? throw new BusinessException("用户不在该租户中");

		_dbContext.TenantUsers.Remove(tenantUser);
		await _dbContext.SaveChangesAsync();
		_logger.LogInformation("Removed user {UserId} from tenant {TenantId}", userId, tenantId);
	}

	public async Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(Guid tenantId)
	{
		// 确认租户存在
		await LoadTenantAsync(tenantId);

		var users = await _dbContext.TenantUsers
			.AsNoTracking()
			.Where(x => x.TenantId == tenantId)
			.Join(_dbContext.Users,
				tu => tu.UserId,
				u => u.Id,
				(tu, u) => new TenantUserDto
				{
					UserId = tu.UserId,
					NickName = u.NickName,
					Email = u.Email,
					RoleInTenant = tu.RoleInTenant,
					JoinedAtUtc = tu.JoinedAtUtc
				})
			.ToListAsync();

		return users;
	}

	public async Task<TenantQuotaDto> GetQuotaAsync(Guid tenantId)
	{
		await LoadTenantAsync(tenantId);

		var quota = await _dbContext.TenantQuotas
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.TenantId == tenantId)
			?? throw new BusinessException("租户配额不存在");

		return new TenantQuotaDto
		{
			TenantId = quota.TenantId,
			MaxUsers = quota.MaxUsers,
			MaxStorageBytes = quota.MaxStorageBytes,
			MaxApiCallsPerDay = quota.MaxApiCallsPerDay,
			MaxFileSize = quota.MaxFileSize,
			MaxOrdersPerDay = quota.MaxOrdersPerDay
		};
	}

	public async Task UpdateQuotaAsync(Guid tenantId, UpdateTenantQuotaInputDto input)
	{
		await LoadTenantAsync(tenantId);

		var quota = await _dbContext.TenantQuotas
			.FirstOrDefaultAsync(x => x.TenantId == tenantId)
			?? throw new BusinessException("租户配额不存在");

		quota.Update(
			input.MaxUsers,
			input.MaxStorageBytes,
			input.MaxApiCallsPerDay,
			input.MaxFileSize,
			input.MaxOrdersPerDay);

		await _dbContext.SaveChangesAsync();
		_logger.LogInformation("Updated quota for tenant {TenantId}", tenantId);
	}

	private async Task<Tenant> LoadTenantAsync(Guid id)
	{
		return await _dbContext.Tenants
			.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
			?? throw new BusinessException("租户不存在");
	}

	private async Task ValidateTenantCodeAsync(string code, Guid? excludeId)
	{
		var exists = await _dbContext.Tenants
			.AnyAsync(x => x.Code == code && x.Id != excludeId && !x.IsDeleted);

		if (exists)
		{
			throw new BusinessException("租户编码已存在");
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
