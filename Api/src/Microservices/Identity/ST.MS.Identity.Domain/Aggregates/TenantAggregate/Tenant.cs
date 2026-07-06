using System.Text.RegularExpressions;
using ST.MS.Identity.Domain.Enums;
using ST.Shared.Exceptions;

namespace ST.MS.Identity.Domain.Aggregates.TenantAggregate;

/// <summary>
/// 租户
/// </summary>
public class Tenant : AggregateRoot, ISoftDelete
{
	public Tenant() { }

	public Tenant(string code, string name)
	{
		if (string.IsNullOrWhiteSpace(code))
			throw new BusinessException("租户编码不能为空");

		if (string.IsNullOrWhiteSpace(name))
			throw new BusinessException("租户名称不能为空");

		if (!Regex.IsMatch(code.Trim(), @"^[a-z][a-z0-9]{1,63}$"))
			throw new DomainException("租户编码只能包含小写字母和数字，且以字母开头，长度 2-64");

		Id = Guid.CreateVersion7();
		Code = code.Trim().ToLowerInvariant();
		Name = name.Trim();
		Status = TenantStatus.Active;
	}

	/// <summary>
	/// 租户编码（唯一标识，如 "acme"）
	/// </summary>
	public string Code { get; private set; } = string.Empty;

	/// <summary>
	/// 租户名称
	/// </summary>
	public string Name { get; private set; } = string.Empty;

	/// <summary>
	/// 租户状态
	/// </summary>
	public TenantStatus Status { get; private set; }

	/// <summary>
	/// 套餐 ID（预留）
	/// </summary>
	public string? PackageId { get; private set; }

	/// <summary>
	/// 过期时间
	/// </summary>
	public DateTime? ExpireAtUtc { get; private set; }

	/// <summary>
	/// 是否已删除
	/// </summary>
	public bool IsDeleted { get; set; }

	/// <summary>
	/// 租户用户关联
	/// </summary>
	public List<TenantUser> TenantUsers { get; set; } = [];

	#region 行为

	/// <summary>
	/// 激活租户
	/// </summary>
	public void Activate()
	{
		Status = TenantStatus.Active;
	}

	/// <summary>
	/// 暂停租户
	/// </summary>
	public void Suspend()
	{
		if (Status == TenantStatus.Deleted)
			throw new DomainException("已注销的租户不能暂停");
		Status = TenantStatus.Suspended;
	}

	/// <summary>
	/// 注销租户
	/// </summary>
	public void Delete()
	{
		Status = TenantStatus.Deleted;
		IsDeleted = true;
	}

	/// <summary>
	/// 更新基本信息
	/// </summary>
	public void UpdateInfo(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new BusinessException("租户名称不能为空");
		Name = name.Trim();
	}

	/// <summary>
	/// 设置套餐
	/// </summary>
	public void SetPackage(string? packageId)
	{
		PackageId = packageId;
	}

	/// <summary>
	/// 设置过期时间
	/// </summary>
	public void SetExpireDate(DateTime? expireAtUtc)
	{
		ExpireAtUtc = expireAtUtc;
	}

	/// <summary>
	/// 是否激活状态
	/// </summary>
	public bool IsActive => Status == TenantStatus.Active;

	#endregion
}
