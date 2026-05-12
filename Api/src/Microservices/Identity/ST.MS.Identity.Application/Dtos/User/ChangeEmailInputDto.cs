namespace ST.MS.Identity.Application.Dtos.User;

public class ChangeEmailInputDto
{
	/// <summary>
	/// 新邮箱
	/// </summary>
	public string NewEmail { get; set; } = string.Empty;

	/// <summary>
	/// 当前邮箱验证码
	/// </summary>
	public string CurrentEmailVerifyCode { get; set; } = string.Empty;

	/// <summary>
	/// 新邮箱验证码
	/// </summary>
	public string NewEmailVerifyCode { get; set; } = string.Empty;
}
