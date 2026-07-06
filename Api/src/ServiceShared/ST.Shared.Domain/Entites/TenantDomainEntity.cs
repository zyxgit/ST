using ST.Infra.Repository.Entities;

namespace ST.Shared.Domain.Entites;

/// <summary>
/// 带租户隔离的领域实体基类。
/// 继承 DomainEntity 并实现 ITenantEntity，新增实体时自动填充 TenantId。
/// </summary>
public abstract class TenantDomainEntity : DomainEntity, ITenantEntity
{
	/// <summary>
	/// 租户 ID
	/// </summary>
	public Guid TenantId { get; set; }
}
