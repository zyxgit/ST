using ST.MS.Identity.Application.Dtos.Tenant;
using ST.Shared.Application.Dtos;

namespace ST.MS.Identity.Application.IServices;

public interface ITenantService : IAppService
{
	/// <summary>
	/// 分页查询租户
	/// </summary>
	Task<PagedResultDto<TenantListItemDto>> GetPageAsync(TenantQueryInputDto input);

	/// <summary>
	/// 租户详情
	/// </summary>
	Task<TenantDetailDto> GetDetailAsync(Guid id);

	/// <summary>
	/// 创建租户
	/// </summary>
	Task<Guid> CreateAsync(CreateTenantInputDto input);

	/// <summary>
	/// 更新租户信息
	/// </summary>
	Task UpdateAsync(Guid id, UpdateTenantInputDto input);

	/// <summary>
	/// 激活租户
	/// </summary>
	Task ActivateAsync(Guid id);

	/// <summary>
	/// 暂停租户
	/// </summary>
	Task SuspendAsync(Guid id);

	/// <summary>
	/// 删除租户
	/// </summary>
	Task DeleteAsync(Guid id);

	/// <summary>
	/// 添加租户用户
	/// </summary>
	Task AddUserAsync(Guid tenantId, AddTenantUserInputDto input);

	/// <summary>
	/// 移除租户用户
	/// </summary>
	Task RemoveUserAsync(Guid tenantId, Guid userId);

	/// <summary>
	/// 查询租户用户列表
	/// </summary>
	Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(Guid tenantId);

	/// <summary>
	/// 查询租户配额
	/// </summary>
	Task<TenantQuotaDto> GetQuotaAsync(Guid tenantId);

	/// <summary>
	/// 更新租户配额
	/// </summary>
	Task UpdateQuotaAsync(Guid tenantId, UpdateTenantQuotaInputDto input);
}
