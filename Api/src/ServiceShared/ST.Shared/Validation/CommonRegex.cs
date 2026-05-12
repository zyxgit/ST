using System.Text.RegularExpressions;

namespace ST.Shared.Validation;

public partial class CommonRegex
{
	/// <summary>
	/// 邮箱
	/// </summary>
	/// <returns></returns>
	[GeneratedRegex(RegexPatterns.Email, RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	public static partial Regex Email();

	/// <summary>
	/// cn手机号
	/// </summary>
	/// <returns></returns>
	[GeneratedRegex(RegexPatterns.ChinaMobile)]
	public static partial Regex ChinaMobile();

	/// <summary>
	/// 用户名
	/// </summary>
	/// <returns></returns>
	[GeneratedRegex(RegexPatterns.UserName)]
	public static partial Regex UserName();

	/// <summary>
	/// 强密码
	/// </summary>
	[GeneratedRegex(RegexPatterns.StrongPassword)]
	public static partial Regex StrongPassword();

	/// <summary>
	/// IPv4
	/// </summary>
	[GeneratedRegex(RegexPatterns.IPv4)]
	public static partial Regex IPv4();

	/// <summary>
	/// URL
	/// </summary>
	[GeneratedRegex(RegexPatterns.Url,
		RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	public static partial Regex Url();

	/// <summary>
	/// 纯数字
	/// </summary>
	[GeneratedRegex(RegexPatterns.Digits)]
	public static partial Regex Digits();
}
