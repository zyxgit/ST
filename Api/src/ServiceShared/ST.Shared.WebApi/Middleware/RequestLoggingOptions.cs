namespace ST.Shared.WebApi.Middleware;

public sealed class RequestLoggingOptions
{
	/// <summary>
	/// 是否记录请求日志（包含 method/path/query 等）。
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// 是否记录请求 Body（默认关闭，避免泄露敏感信息/大请求体导致性能问题）。
	/// </summary>
	public bool LogBody { get; set; } = false;

	/// <summary>
	/// Body 读取/输出的最大字符数（仅在启用 LogBody 时生效）。
	/// </summary>
	public int MaxBodyChars { get; set; } = 4096;

	/// <summary>
	/// 仅当 Content-Type 命中其中之一时才允许记录 Body。
	/// </summary>
	public string[] AllowedContentTypes { get; set; } =
	[
		"application/json",
		"application/x-www-form-urlencoded"
	];

	/// <summary>
	/// 记录 Body 时，若是 JSON 则对敏感 key 做脱敏。
	/// </summary>
	public bool MaskJsonBody { get; set; } = true;

	/// <summary>
	/// JSON 脱敏时的替换值。
	/// </summary>
	public string Mask { get; set; } = "****";

	/// <summary>
	/// JSON 脱敏的敏感字段 key（大小写不敏感）。
	/// </summary>
	public string[] SensitiveKeys { get; set; } =
	[
		"password",
		"pwd",
		"token",
		"accessToken",
		"refreshToken",
		"secret",
		"authorization"
	];
}

