using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using ST.MS.FileUpload.Domain.Services;

namespace ST.MS.FileUpload.Infra.Services;

/// <summary>
/// 签名 URL 服务实现。
/// 使用 HMAC-SHA256 生成和验证签名。
/// </summary>
public sealed class SignedUrlService : ISignedUrlService
{
	private readonly string _secretKey;
	private readonly string _baseUrl;

	/// <summary>
	/// 签名 URL 路径前缀
	/// </summary>
	private const string SignedUrlPath = "/api/files/signed";

	public SignedUrlService(IConfiguration configuration)
	{
		var secretKey = configuration["SignedUrl:SecretKey"];
		if (string.IsNullOrWhiteSpace(secretKey)
		    || secretKey == "default-secret-key-change-in-production"
		    || secretKey.StartsWith("CHANGE-ME", StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException(
				"SignedUrl:SecretKey 未配置或使用了占位符，请在 appsettings.json 或环境变量中设置一个安全的密钥。");

		_secretKey = secretKey;
		_baseUrl = configuration["SignedUrl:BaseUrl"] ?? "";
	}

	/// <inheritdoc />
	public SignedUrlResult GenerateSignedUrl(Guid fileId, int expiresIn = 3600, Guid? userId = null)
	{
		if (expiresIn <= 0 || expiresIn > 86400) // 最大 24 小时
			expiresIn = 3600;

		var expiresAtUtc = DateTime.UtcNow.AddSeconds(expiresIn);

		// 构建签名负载：fileId:expiresAt:userId
		var payload = $"{fileId}:{expiresAtUtc.Ticks}:{userId}";

		// 计算签名
		var signature = ComputeHmacSha256(payload, _secretKey);

		// 构建令牌（Base64 编码的负载）
		var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
			.Replace('+', '-')
			.Replace('/', '_')
			.TrimEnd('=');

		// 构建完整 URL
		var url = $"{_baseUrl}{SignedUrlPath}/{token}?sig={signature}";

		return new SignedUrlResult
		{
			Url = url,
			ExpiresAtUtc = expiresAtUtc,
			ExpiresIn = expiresIn
		};
	}

	/// <inheritdoc />
	public SignedUrlValidationResult ValidateSignedUrl(string token, string signature)
	{
		if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(signature))
			return SignedUrlValidationResult.Failure("令牌或签名为空");

		try
		{
			// 解码令牌
			var paddedToken = token
				.Replace('-', '+')
				.Replace('_', '/');
			switch (paddedToken.Length % 4)
			{
				case 2: paddedToken += "=="; break;
				case 3: paddedToken += "="; break;
			}

			var payloadBytes = Convert.FromBase64String(paddedToken);
			var payload = Encoding.UTF8.GetString(payloadBytes);

			// 验证签名
			var expectedSignature = ComputeHmacSha256(payload, _secretKey);
			if (!string.Equals(signature, expectedSignature, StringComparison.Ordinal))
				return SignedUrlValidationResult.Failure("签名验证失败");

			// 解析负载
			var parts = payload.Split(':');
			if (parts.Length != 3)
				return SignedUrlValidationResult.Failure("无效的令牌格式");

			if (!Guid.TryParse(parts[0], out var fileId))
				return SignedUrlValidationResult.Failure("无效的文件 ID");

			if (!long.TryParse(parts[1], out var ticks))
				return SignedUrlValidationResult.Failure("无效的过期时间");

			var expiresAtUtc = new DateTime(ticks, DateTimeKind.Utc);

			// 检查是否过期
			if (expiresAtUtc < DateTime.UtcNow)
				return SignedUrlValidationResult.Failure("签名 URL 已过期");

			Guid? userId = null;
			if (Guid.TryParse(parts[2], out var parsedUserId))
				userId = parsedUserId;

			return SignedUrlValidationResult.Success(fileId, userId, expiresAtUtc);
		}
		catch (FormatException)
		{
			return SignedUrlValidationResult.Failure("无效的令牌格式");
		}
		catch (Exception ex)
		{
			return SignedUrlValidationResult.Failure($"验证失败: {ex.Message}");
		}
	}

	/// <summary>
	/// 使用 HMAC-SHA256 计算签名。
	/// </summary>
	private static string ComputeHmacSha256(string data, string key)
	{
		var keyBytes = Encoding.UTF8.GetBytes(key);
		var dataBytes = Encoding.UTF8.GetBytes(data);

		using var hmac = new HMACSHA256(keyBytes);
		var hashBytes = hmac.ComputeHash(dataBytes);

		return Convert.ToBase64String(hashBytes);
	}
}
