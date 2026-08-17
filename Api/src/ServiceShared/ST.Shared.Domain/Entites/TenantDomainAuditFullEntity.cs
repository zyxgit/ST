using ST.Infra.Repository.Entities;

namespace ST.Shared.Domain.Entites;

/// <summary>
/// 带租户隔离和完整审计的领域实体基类。
/// 继承 DomainAuditFullEntity 并实现 ITenantEntity，新增实体时自动填充 TenantId 和审计字段。
/// </summary>
public abstract class TenantDomainAuditFullEntity : DomainAuditFullEntity, ITenantEntity
{
	/// <summary>
	/// 租户 ID
	/// </summary>
	public Guid TenantId { get; set; }
}
