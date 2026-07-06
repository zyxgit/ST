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

	/// <summary>
	/// 租户编码（可选，不填则不绑定租户）
	/// </summary>
	public string? TenantCode { get; set; }
}
