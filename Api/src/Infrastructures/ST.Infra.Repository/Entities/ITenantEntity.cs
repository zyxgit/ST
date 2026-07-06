namespace ST.Infra.Repository.Entities;

/// <summary>
/// 标记接口：实现此接口的实体将自动参与租户数据隔离。
/// - EF Core 全局查询过滤器自动附加 WHERE tenant_id = @currentTenantId
/// - 新增实体时自动填充 TenantId
/// </summary>
public interface ITenantEntity
{
	/// <summary>
	/// 租户 ID
	/// </summary>
	Guid TenantId { get; set; }
}
