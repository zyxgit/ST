namespace ST.MS.Identity.Application.Dtos.Tenant;

/// <summary>
/// 创建租户
/// </summary>
public sealed class CreateTenantInputDto
{
	/// <summary>
	/// 租户编码（唯一，小写字母+数字，2-64 位）
	/// </summary>
	public string Code { get; set; } = string.Empty;

	/// <summary>
	/// 租户名称
	/// </summary>
	public string Name { get; set; } = string.Empty;
}
