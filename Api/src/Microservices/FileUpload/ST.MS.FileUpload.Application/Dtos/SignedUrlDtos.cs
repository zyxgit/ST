namespace ST.MS.FileUpload.Application.Dtos;

/// <summary>
/// 生成签名 URL 请求。
/// </summary>
public sealed class GenerateSignedUrlRequestDto
{
	/// <summary>文件 ID</summary>
	public Guid FileId { get; set; }

	/// <summary>过期时间（秒，默认 3600 即 1 小时）</summary>
	public int ExpiresIn { get; set; } = 3600;
}

/// <summary>
/// 生成签名 URL 结果。
/// </summary>
public sealed class GenerateSignedUrlResultDto
{
	/// <summary>签名 URL</summary>
	public string Url { get; set; } = string.Empty;

	/// <summary>过期时间（UTC）</summary>
	public DateTime ExpiresAtUtc { get; set; }

	/// <summary>过期时间（秒）</summary>
	public int ExpiresIn { get; set; }
}
