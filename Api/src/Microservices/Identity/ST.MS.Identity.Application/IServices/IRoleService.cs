using ST.MS.Identity.Application.Dtos.Role;
using ST.Shared.Application.Dtos;

namespace ST.MS.Identity.Application.IServices;

public interface IRoleService : IAppService
{
	Task<PagedResultDto<RoleListItemDto>> GetPageAsync(RoleQueryInputDto input);

	Task<RoleDetailDto> GetDetailAsync(Guid id);

	Task<Guid> CreateAsync(CreateRoleInputDto input);

	Task UpdateAsync(Guid id, UpdateRoleInputDto input);

	Task ChangePermissionsAsync(Guid id, ChangeRolePermissionsInputDto input);

	Task DeleteAsync(Guid id);
}
