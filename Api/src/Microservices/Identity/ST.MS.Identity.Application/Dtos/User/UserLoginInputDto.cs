namespace ST.MS.Identity.Application.Dtos.User;

public class UserLoginInputDto
{
	/// <summary>
	/// 邮箱
	/// </summary>
	public string Email { get; set; } = string.Empty;

	/// <summary>
	/// 密码
	/// </summary>
	public string Password { get; set; } = string.Empty;
}
