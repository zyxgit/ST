namespace ST.Shared.Security;

/// <summary>
/// 当前租户上下文访问器
/// </summary>
public interface ICurrentTenantAccessor
{
	/// <summary>
	/// 当前租户 ID（null 表示未指定租户或超级管理员场景）
	/// </summary>
	Guid? TenantId { get; }

	/// <summary>
	/// 当前租户编码
	/// </summary>
	string? TenantCode { get; }
}
