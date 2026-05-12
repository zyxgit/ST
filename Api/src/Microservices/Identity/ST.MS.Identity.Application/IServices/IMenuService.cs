using ST.MS.Identity.Application.Dtos.Menu;

namespace ST.MS.Identity.Application.IServices;

public interface IMenuService : IAppService
{
	Task<IReadOnlyList<MenuTreeNodeDto>> GetTreeAsync();

	Task<IReadOnlyList<MenuTreeNodeDto>> GetCurrentUserTreeAsync();

	Task<MenuDetailDto> GetDetailAsync(Guid id);

	Task<Guid> CreateAsync(CreateMenuInputDto input);

	Task UpdateAsync(Guid id, UpdateMenuInputDto input);

	Task DeleteAsync(Guid id);
}
