using ST.Infra.Repository.Interface;

namespace ST.MS.OperationLog.Consumer.Infrastructure;

/// <summary>
/// 后台消费进程没有 HTTP 用户上下文，返回 null 让基础设施层回退为 Guid.Empty。
/// </summary>
public sealed class BackgroundCurrentUserIdAccessor : ICurrentUserIdAccessor
{
	public Guid? UserId => null;
}
