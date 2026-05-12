using ST.MS.Identity.Domain.Enums;

namespace ST.MS.Identity.Application.Dtos.User;

public class SendEmailInputDto
{
	/// <summary>
	/// 邮箱
	/// </summary>
	public string Email { get; set; } = string.Empty;

	/// <summary>
	/// 验证码用途
	/// </summary>
	public CodePurpose CodePurpose { get; set; }
}
