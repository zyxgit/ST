namespace ST.MS.Identity.Domain.Enums;

/// <summary>
/// 验证码用途
/// </summary>
public enum CodePurpose
{
	/// <summary>
	/// 验证码
	/// </summary>
	Code,
	/// <summary>
	/// 注册
	/// </summary>
	Register,
	/// <summary>
	/// 登录
	/// </summary>
	Login,
	/// <summary>
	/// 重置密码
	/// </summary>
	ResetPassword,
	/// <summary>
	/// 修改邮箱
	/// </summary>
	ChangeEmail,
}
