namespace ST.Shared.Validation;

public class RegexPatterns
{
	/// <summary>
	/// Email（RFC 简化版，90% 业务足够）
	/// </summary>
	public const string Email = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

	/// <summary>
	/// 中国大陆手机号（1[3-9]）
	/// </summary>
	public const string ChinaMobile = @"^1[3-9]\d{9}$";

	/// <summary>
	/// 用户名（字母开头，4-20 位，字母数字下划线）
	/// </summary>
	public const string UserName = @"^[a-zA-Z][a-zA-Z0-9_]{3,19}$";

	/// <summary>
	/// 强密码（至少 1 大写 + 1 小写 + 1 数字，8-32 位）
	/// </summary>
	public const string StrongPassword = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,32}$";

	/// <summary>
	/// IPv4
	/// </summary>
	public const string IPv4 = @"^(25[0-5]|2[0-4]\d|[01]?\d\d?)\." + @"(25[0-5]|2[0-4]\d|[01]?\d\d?)\." + @"(25[0-5]|2[0-4]\d|[01]?\d\d?)\." + @"(25[0-5]|2[0-4]\d|[01]?\d\d?)$";

	/// <summary>
	/// URL（http / https）
	/// </summary>
	public const string Url = @"^https?:\/\/([\w\-]+\.)+[\w\-]+(\/[\w\-._~:/?#[\]@!$&'()*+,;=]*)?$";

	/// <summary>
	/// 纯数字
	/// </summary>
	public const string Digits = @"^\d+$";
}
