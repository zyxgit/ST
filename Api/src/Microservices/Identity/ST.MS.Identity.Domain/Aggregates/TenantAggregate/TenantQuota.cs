using ST.Shared.Exceptions;

namespace ST.MS.Identity.Domain.Aggregates.TenantAggregate;

/// <summary>
/// 租户配额
/// </summary>
public class TenantQuota : AggregateRoot
{
	public TenantQuota() { }

	public TenantQuota(Guid tenantId)
	{
		Id = Guid.CreateVersion7();
		TenantId = tenantId;
		// 默认配额
		MaxUsers = 100;
		MaxStorageBytes = 10L * 1024 * 1024 * 1024; // 10 GB
		MaxApiCallsPerDay = 100_000;
		MaxFileSize = 100L * 1024 * 1024; // 100 MB
		MaxOrdersPerDay = 10_000;
	}

	/// <summary>
	/// 租户 ID
	/// </summary>
	public Guid TenantId { get; set; }

	/// <summary>
	/// 用户数上限
	/// </summary>
	public int MaxUsers { get; set; }

	/// <summary>
	/// 存储容量上限（字节）
	/// </summary>
	public long MaxStorageBytes { get; set; }

	/// <summary>
	/// 每日 API 调用上限
	/// </summary>
	public int MaxApiCallsPerDay { get; set; }

	/// <summary>
	/// 单文件大小上限（字节）
	/// </summary>
	public long MaxFileSize { get; set; }

	/// <summary>
	/// 每日订单上限
	/// </summary>
	public int MaxOrdersPerDay { get; set; }

	#region 行为

	/// <summary>
	/// 更新配额
	/// </summary>
	public void Update(
		int? maxUsers = null,
		long? maxStorageBytes = null,
		int? maxApiCallsPerDay = null,
		long? maxFileSize = null,
		int? maxOrdersPerDay = null)
	{
		if (maxUsers.HasValue)
		{
			if (maxUsers.Value < 1)
				throw new BusinessException("用户数上限不能小于 1");
			MaxUsers = maxUsers.Value;
		}

		if (maxStorageBytes.HasValue)
		{
			if (maxStorageBytes.Value < 0)
				throw new BusinessException("存储容量上限不能为负数");
			MaxStorageBytes = maxStorageBytes.Value;
		}

		if (maxApiCallsPerDay.HasValue)
		{
			if (maxApiCallsPerDay.Value < 0)
				throw new BusinessException("每日 API 调用上限不能为负数");
			MaxApiCallsPerDay = maxApiCallsPerDay.Value;
		}

		if (maxFileSize.HasValue)
		{
			if (maxFileSize.Value < 0)
				throw new BusinessException("单文件大小上限不能为负数");
			MaxFileSize = maxFileSize.Value;
		}

		if (maxOrdersPerDay.HasValue)
		{
			if (maxOrdersPerDay.Value < 0)
				throw new BusinessException("每日订单上限不能为负数");
			MaxOrdersPerDay = maxOrdersPerDay.Value;
		}
	}

	#endregion
}
