using ST.Infra.Repository.Entities;

namespace ST.Shared.Domain.Entites;

public abstract class DomainAuditFullEntity : DomainEntity, IFullAuditInfo
{
    /// <summary>
    /// 修改人
    /// </summary>
    public Guid ModifyBy { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifyTime { get; set; }
}
