namespace ST.Shared.WebApi.Authentication;

public sealed class JwtOptions
{
	public string Issuer { get; set; } = "st";

	public string Audience { get; set; } = "st";

	// 生产环境务必使用强密钥（HMAC 建议 32+ 字节）。
	public string SigningKey { get; set; } = string.Empty;

	public int? AccessTokenSeconds { get; set; }

	public int AccessTokenMinutes { get; set; } = 60;
}
