namespace ST.MS.Identity.Application.Dtos.Tenant;

/// <summary>
/// 更新租户配额（所有字段可选，仅更新传入的字段）
/// </summary>
public sealed class UpdateTenantQuotaInputDto
{
	/// <summary>
	/// 用户数上限
	/// </summary>
	public int? MaxUsers { get; set; }

	/// <summary>
	/// 存储容量上限（字节）
	/// </summary>
	public long? MaxStorageBytes { get; set; }

	/// <summary>
	/// 每日 API 调用上限
	/// </summary>
	public int? MaxApiCallsPerDay { get; set; }

	/// <summary>
	/// 单文件大小上限（字节）
	/// </summary>
	public long? MaxFileSize { get; set; }

	/// <summary>
	/// 每日订单上限
	/// </summary>
	public int? MaxOrdersPerDay { get; set; }
}
