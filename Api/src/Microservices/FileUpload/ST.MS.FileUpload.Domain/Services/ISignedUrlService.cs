namespace ST.MS.FileUpload.Domain.Services;

/// <summary>
/// 签名 URL 服务接口。
/// 用于生成和验证私有文件的短期有效下载链接。
/// </summary>
public interface ISignedUrlService
{
	/// <summary>
	/// 生成签名下载 URL。
	/// </summary>
	/// <param name="fileId">文件 ID</param>
	/// <param name="expiresIn">过期时间（秒）</param>
	/// <param name="userId">用户 ID（可选，用于权限校验）</param>
	/// <returns>签名 URL 和过期时间</returns>
	SignedUrlResult GenerateSignedUrl(Guid fileId, int expiresIn = 3600, Guid? userId = null);

	/// <summary>
	/// 验证签名 URL。
	/// </summary>
	/// <param name="token">签名令牌</param>
	/// <param name="signature">签名</param>
	/// <returns>验证结果，包含文件 ID</returns>
	SignedUrlValidationResult ValidateSignedUrl(string token, string signature);
}

/// <summary>
/// 签名 URL 结果。
/// </summary>
public sealed class SignedUrlResult
{
	/// <summary>签名 URL</summary>
	public string Url { get; set; } = string.Empty;

	/// <summary>过期时间（UTC）</summary>
	public DateTime ExpiresAtUtc { get; set; }

	/// <summary>过期时间（秒）</summary>
	public int ExpiresIn { get; set; }
}

/// <summary>
/// 签名 URL 验证结果。
/// </summary>
public sealed class SignedUrlValidationResult
{
	/// <summary>是否有效</summary>
	public bool IsValid { get; set; }

	/// <summary>文件 ID</summary>
	public Guid FileId { get; set; }

	/// <summary>用户 ID</summary>
	public Guid? UserId { get; set; }

	/// <summary>过期时间（UTC）</summary>
	public DateTime ExpiresAtUtc { get; set; }

	/// <summary>错误信息</summary>
	public string? ErrorMessage { get; set; }

	/// <summary>创建成功结果</summary>
	public static SignedUrlValidationResult Success(Guid fileId, Guid? userId, DateTime expiresAtUtc) => new()
	{
		IsValid = true,
		FileId = fileId,
		UserId = userId,
		ExpiresAtUtc = expiresAtUtc
	};

	/// <summary>创建失败结果</summary>
	public static SignedUrlValidationResult Failure(string error) => new()
	{
		IsValid = false,
		ErrorMessage = error
	};
}
