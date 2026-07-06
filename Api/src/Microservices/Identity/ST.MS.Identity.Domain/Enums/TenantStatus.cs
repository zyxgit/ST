namespace ST.MS.Identity.Domain.Enums;

/// <summary>
/// 租户状态
/// </summary>
public enum TenantStatus
{
	/// <summary>
	/// 正常
	/// </summary>
	Active = 0,

	/// <summary>
	/// 暂停（欠费/违规）
	/// </summary>
	Suspended = 1,

	/// <summary>
	/// 已注销
	/// </summary>
	Deleted = 2,
}
