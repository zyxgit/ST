using ST.MS.Identity.Application.Dtos.User;
using ST.Shared.Application.Dtos;

namespace ST.MS.Identity.Application.IServices;

public interface IUserService : IAppService
{
	/// <summary>
	/// 注册
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	Task RegisterAsync(RegisterInputDto input);

	/// <summary>
	/// 发送邮箱验证码
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	Task SendEmailCodeAsync(SendEmailInputDto input);

	/// <summary>
	/// 登录（返回 AccessToken + RefreshToken）
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	Task<LoginResultDto> LoginAsync(UserLoginInputDto input);

	/// <summary>
	/// 刷新 Token（旋转 RefreshToken）
	/// </summary>
	Task<LoginResultDto> RefreshTokenAsync(RefreshTokenInputDto input);

	/// <summary>
	/// 注销（撤销 RefreshToken）
	/// </summary>
	Task LogoutAsync(RefreshTokenInputDto input);

	Task<IReadOnlyList<UserOptionDto>> GetUsersAsync();

	Task<IReadOnlyList<RoleOptionDto>> GetRoleOptionsAsync();

	Task<PagedResultDto<UserListItemDto>> GetUserPageAsync(UserQueryInputDto input);

	Task<UserDetailDto> GetUserDetailAsync(Guid id);

	Task<Guid> CreateUserAsync(CreateUserInputDto input);

	Task UpdateUserAsync(Guid id, UpdateUserInputDto input);

	Task<bool> EmailExistsAsync(string email, Guid? excludeUserId);

	Task ChangeEmailAsync(ChangeEmailInputDto input);

	Task ChangeUserStatusAsync(Guid id, ChangeUserStatusInputDto input);

	Task ResetPasswordAsync(Guid id, ResetUserPasswordInputDto input);

	Task DeleteUserAsync(Guid id);

	Task SetUserAvatarAsync(Guid id, SetUserAvatarInputDto input);

	Task DeleteUserAvatarAsync(Guid id);
}
