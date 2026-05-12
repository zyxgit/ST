namespace ST.MS.Identity.Application.Dtos.User;

public class RegisterInputDto
{
	/// <summary>
	///  邮箱
	/// </summary>
	public string Email { get; set; } = string.Empty;

	/// <summary>
	/// 密码
	/// </summary>
	public string Password { get; set; } = string.Empty;

	/// <summary>
	/// 邮箱验证码
	/// </summary>
	public string EmailVerifyCode { get; set; } = string.Empty;
}
