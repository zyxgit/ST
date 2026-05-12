namespace ST.Infra.Repository.Interface;

/// <summary>
/// 当前操作用户（用于基础设施层自动填充审计字段）
/// </summary>
public interface ICurrentUserIdAccessor
{
	Guid? UserId { get; }
}
