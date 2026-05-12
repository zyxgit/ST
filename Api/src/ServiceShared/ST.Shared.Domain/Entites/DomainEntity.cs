using ST.Infra.Repository.Entities;

namespace ST.Shared.Domain.Entites;

public abstract class DomainEntity : Entity, IBasicAuditInfo
{
    public Guid CreateBy { get; set; }
    public DateTime CreateTime { get; set; }
}
