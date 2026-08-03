using ST.MS.Identity.Application.Dtos.User;
using ST.MS.Identity.Application.IServices;
using ST.Shared.Security;

namespace ST.MS.Identity.Api.Controllers;

/// <summary>
/// 用户管理
/// </summary>
public class UserController : AbstractControllerBase
{
	private readonly IUserService _userService;
	private readonly IUserContext _userContext;

	/// <summary>
	/// 构造函数
	/// </summary>
	public UserController(
		IUserService userService,
		IUserContext userContext)
	{
		_userService = userService;
		_userContext = userContext;
	}

	/// <summary>
	/// 发送邮件
	/// </summary>
	[HttpPost("email")]
	[AllowAnonymous]
	[OperationLog("发送邮箱验证码", RecordRequest = true, RecordResponse = false)]
	public async Task SendEmail(SendEmailInputDto input)
	{
		await _userService.SendEmailCodeAsync(input);
	}

	/// <summary>
	/// 注册
	/// </summary>
	[HttpPost("register")]
	[AllowAnonymous]
	[OperationLog("用户注册", RecordRequest = true, RecordResponse = false)]
	public async Task Register(RegisterInputDto input)
	{
		await _userService.RegisterAsync(input);
	}

	/// <summary>
	/// 登录（返回 AccessToken + RefreshToken）
	/// </summary>
	[AllowAnonymous]
	[HttpPost("login")]
	[OperationLog("用户登录", RecordRequest = true, RecordResponse = false)]
	public async Task<IActionResult> Login(UserLoginInputDto input)
	{
		var result = await _userService.LoginAsync(input);
		return Ok(result);
	}

	/// <summary>
	/// 刷新 Token（旋转 RefreshToken）
	/// </summary>
	[AllowAnonymous]
	[HttpPost("refresh")]
	[OperationLog("刷新令牌", RecordRequest = true, RecordResponse = false)]
	public async Task<IActionResult> Refresh(RefreshTokenInputDto input)
	{
		var result = await _userService.RefreshTokenAsync(input);
		return Ok(result);
	}

	/// <summary>
	/// 注销（撤销 RefreshToken）
	/// </summary>
	[AllowAnonymous]
	[HttpPost("logout")]
	[OperationLog("用户注销", RecordRequest = true, RecordResponse = false)]
	public async Task Logout(RefreshTokenInputDto input)
	{
		await _userService.LogoutAsync(input);
	}

	/// <summary>
	/// 用户下拉选项
	/// </summary>
	[HttpGet("users/options")]
	[PermissionAuthorize(Permission.UserQuery)]
	public async Task<IActionResult> Users()
	{
		var list = await _userService.GetUsersAsync();
		return Ok(list);
	}

	/// <summary>
	/// 角色下拉选项
	/// </summary>
	[HttpGet("roles/options")]
	[PermissionAuthorize(Permission.RoleQuery)]
	public async Task<IActionResult> RoleOptions()
	{
		var list = await _userService.GetRoleOptionsAsync();
		return Ok(list);
	}

	/// <summary>
	/// 用户分页查询
	/// </summary>
	[HttpGet("users")]
	[PermissionAuthorize(Permission.UserQuery)]
	public async Task<IActionResult> GetUserPage([FromQuery] UserQueryInputDto input)
	{
		var result = await _userService.GetUserPageAsync(input);
		return Ok(result);
	}

	/// <summary>
	/// 用户详情
	/// </summary>
	[HttpGet("users/{id:guid}")]
	[PermissionAuthorize(Permission.UserQuery)]
	public async Task<IActionResult> GetUser(Guid id)
	{
		var result = await _userService.GetUserDetailAsync(id);
		return Ok(result);
	}

	/// <summary>
	/// 新增用户
	/// </summary>
	[HttpPost("users")]
	[PermissionAuthorize(Permission.UserCreate)]
	[OperationLog("新增用户", RecordRequest = true, RecordResponse = false)]
	public async Task<IActionResult> CreateUser(CreateUserInputDto input)
	{
		var id = await _userService.CreateUserAsync(input);
		return Ok(new { Id = id });
	}

	/// <summary>
	/// 编辑用户
	/// </summary>
	[HttpPut("users/{id:guid}")]
	[PermissionAuthorize(Permission.UserUpdate)]
	[OperationLog("编辑用户", RecordRequest = true, RecordResponse = false)]
	public async Task UpdateUser(Guid id, UpdateUserInputDto input)
	{
		await _userService.UpdateUserAsync(id, input);
	}

	/// <summary>
	/// 检查邮箱是否已存在
	/// </summary>
	[HttpGet("users/email-exists")]
	[PermissionAuthorize(Permission.UserQuery)]
	public async Task<IActionResult> EmailExists([FromQuery] string email, [FromQuery] Guid? excludeUserId = null)
	{
		var exists = await _userService.EmailExistsAsync(email, excludeUserId);
		return Ok(new { Exists = exists });
	}

	/// <summary>
	/// 当前用户修改邮箱
	/// </summary>
	[HttpPut("me/email")]
	[OperationLog("修改当前用户邮箱", RecordRequest = true, RecordResponse = false)]
	public async Task ChangeEmail(ChangeEmailInputDto input)
	{
		await _userService.ChangeEmailAsync(input);
	}

	/// <summary>
	/// 当前用户修改密码
	/// </summary>
	[HttpPut("me/password")]
	[OperationLog("修改当前用户密码", RecordRequest = false, RecordResponse = false)]
	public async Task ChangePassword(ChangePasswordInputDto input)
	{
		await _userService.ChangePasswordAsync(input);
	}

	/// <summary>
	/// 启用/禁用用户
	/// </summary>
	[HttpPut("users/{id:guid}/status")]
	[PermissionAuthorize(Permission.UserChangeStatus)]
	[OperationLog("变更用户状态", RecordRequest = true, RecordResponse = false)]
	public async Task ChangeUserStatus(Guid id, ChangeUserStatusInputDto input)
	{
		await _userService.ChangeUserStatusAsync(id, input);
	}

	/// <summary>
	/// 重置密码
	/// </summary>
	[HttpPut("users/{id:guid}/password/reset")]
	[PermissionAuthorize(Permission.UserResetPassword)]
	[OperationLog("重置用户密码", RecordRequest = true, RecordResponse = false)]
	public async Task ResetPassword(Guid id, ResetUserPasswordInputDto input)
	{
		await _userService.ResetPasswordAsync(id, input);
	}

	/// <summary>
	/// 删除用户
	/// </summary>
	[HttpDelete("users/{id:guid}")]
	[PermissionAuthorize(Permission.UserDelete)]
	[OperationLog("删除用户", RecordRequest = true, RecordResponse = false)]
	public async Task DeleteUser(Guid id)
	{
		await _userService.DeleteUserAsync(id);
	}

	/// <summary>
	/// 设置头像
	/// </summary>
	[HttpPut("users/{id:guid}/avatar")]
	[OperationLog("设置用户头像", RecordRequest = true, RecordResponse = false)]
	public async Task SetAvatar(Guid id, SetUserAvatarInputDto input)
	{
		await _userService.SetUserAvatarAsync(id, input);
	}

	/// <summary>
	/// 删除头像
	/// </summary>
	[HttpDelete("users/{id:guid}/avatar")]
	[OperationLog("删除用户头像", RecordRequest = true, RecordResponse = false)]
	public async Task DeleteAvatar(Guid id)
	{
		await _userService.DeleteUserAvatarAsync(id);
	}

	/// <summary>
	/// 当前登录用户信息
	/// </summary>
	[HttpGet("me")]
	public async Task<IActionResult> Me()
	{
		Guid? avatarFileId = null;
		if (_userContext.UserId.HasValue)
		{
			var user = await _userService.GetUserDetailAsync(_userContext.UserId.Value);
			avatarFileId = user.AvatarFileId;
		}

		return Ok(new
		{
			_userContext.IsAuthenticated,
			_userContext.UserId,
			_userContext.Email,
			_userContext.NickName,
			AvatarFileId = avatarFileId,
			_userContext.Roles,
			_userContext.Permissions,
			_userContext.ClientIp
		});
	}
}
