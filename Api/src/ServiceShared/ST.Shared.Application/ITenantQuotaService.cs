namespace ST.Shared.Application;

/// <summary>
/// 租户配额检查服务接口。
/// 各业务服务通过此接口检查配额，实现跨服务配额管理。
/// </summary>
public interface ITenantQuotaService
{
	/// <summary>
	/// 检查并递增每日订单计数。超限抛出 BusinessException。
	/// </summary>
	Task CheckOrderQuotaAsync(Guid tenantId, CancellationToken ct = default);

	/// <summary>
	/// 检查单文件大小限制。超限抛出 BusinessException。
	/// </summary>
	Task CheckFileSizeQuotaAsync(Guid tenantId, long fileSize, CancellationToken ct = default);

	/// <summary>
	/// 检查存储容量限制。超限抛出 BusinessException。
	/// </summary>
	Task CheckStorageQuotaAsync(Guid tenantId, long additionalBytes, CancellationToken ct = default);
}
