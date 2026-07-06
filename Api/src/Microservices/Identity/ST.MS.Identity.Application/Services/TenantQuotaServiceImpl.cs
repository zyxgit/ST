using Microsoft.EntityFrameworkCore;
using ST.MS.Identity.Infra.DbContext;
using ST.Shared.Application;
using ST.Shared.Application.Services;
using ST.Shared.Exceptions;
using StackExchange.Redis;

namespace ST.MS.Identity.Application.Services;

/// <summary>
/// 租户配额检查服务实现。
/// 从 IdentityDbContext 查询配额限制，从 Redis 读取缓存。
/// </summary>
public sealed class TenantQuotaServiceImpl : AbstractAppService, ITenantQuotaService
{
	private readonly IdentityDbContext _dbContext;
	private readonly IDatabase _redis;

	private const string QuotaCachePrefix = "t:tenant:quota";

	public TenantQuotaServiceImpl(IdentityDbContext dbContext, ST.Infra.Redis.Cache.IRedisCacheManager redisCacheManager)
	{
		_dbContext = dbContext;
		_redis = redisCacheManager.GetDatabase();
	}

	public async Task CheckOrderQuotaAsync(Guid tenantId, CancellationToken ct = default)
	{
		var maxOrders = await GetMaxOrdersPerDayAsync(tenantId, ct);
		if (maxOrders <= 0) return;

		var today = DateTime.UtcNow.Date;
		var key = $"{QuotaCachePrefix}:{tenantId}:orders:{today:yyyyMMdd}";
		var count = await _redis.StringIncrementAsync(key);
		if (count == 1)
		{
			await _redis.KeyExpireAsync(key, TimeSpan.FromDays(1));
		}

		if (count > maxOrders)
		{
			throw new BusinessException($"今日订单数已达租户上限（{maxOrders} 单/天）");
		}
	}

	public async Task CheckFileSizeQuotaAsync(Guid tenantId, long fileSize, CancellationToken ct = default)
	{
		var maxFileSize = await GetMaxFileSizeAsync(tenantId, ct);
		if (maxFileSize <= 0) return;

		if (fileSize > maxFileSize)
		{
			var maxMb = maxFileSize / (1024 * 1024);
			throw new BusinessException($"文件大小超过租户限制（最大 {maxMb} MB）");
		}
	}

	public async Task CheckStorageQuotaAsync(Guid tenantId, long additionalBytes, CancellationToken ct = default)
	{
		var maxStorage = await GetMaxStorageBytesAsync(tenantId, ct);
		if (maxStorage <= 0) return;

		var usedKey = $"{QuotaCachePrefix}:{tenantId}:storage_used";
		var used = await _redis.StringGetAsync(usedKey);
		var usedBytes = used.HasValue ? (long)used : 0;

		if (usedBytes + additionalBytes > maxStorage)
		{
			var maxGb = maxStorage / (1024 * 1024 * 1024);
			throw new BusinessException($"存储空间已达租户上限（最大 {maxGb} GB）");
		}
	}

	private async Task<int> GetMaxOrdersPerDayAsync(Guid tenantId, CancellationToken ct)
	{
		var cached = await _redis.StringGetAsync($"{QuotaCachePrefix}:{tenantId}:max_orders");
		if (cached.HasValue) return (int)cached;

		var quota = await _dbContext.TenantQuotas.AsNoTracking()
			.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
		if (quota is null) return 0;

		await _redis.StringSetAsync($"{QuotaCachePrefix}:{tenantId}:max_orders", quota.MaxOrdersPerDay, TimeSpan.FromHours(1));
		return quota.MaxOrdersPerDay;
	}

	private async Task<long> GetMaxFileSizeAsync(Guid tenantId, CancellationToken ct)
	{
		var cached = await _redis.StringGetAsync($"{QuotaCachePrefix}:{tenantId}:max_file_size");
		if (cached.HasValue) return (long)cached;

		var quota = await _dbContext.TenantQuotas.AsNoTracking()
			.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
		if (quota is null) return 0;

		await _redis.StringSetAsync($"{QuotaCachePrefix}:{tenantId}:max_file_size", quota.MaxFileSize, TimeSpan.FromHours(1));
		return quota.MaxFileSize;
	}

	private async Task<long> GetMaxStorageBytesAsync(Guid tenantId, CancellationToken ct)
	{
		var cached = await _redis.StringGetAsync($"{QuotaCachePrefix}:{tenantId}:max_storage");
		if (cached.HasValue) return (long)cached;

		var quota = await _dbContext.TenantQuotas.AsNoTracking()
			.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
		if (quota is null) return 0;

		await _redis.StringSetAsync($"{QuotaCachePrefix}:{tenantId}:max_storage", quota.MaxStorageBytes, TimeSpan.FromHours(1));
		return quota.MaxStorageBytes;
	}
}
