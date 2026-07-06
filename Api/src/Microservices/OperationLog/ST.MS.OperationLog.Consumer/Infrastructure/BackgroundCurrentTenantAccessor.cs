using ST.Shared.Security;

namespace ST.MS.OperationLog.Consumer.Infrastructure;

/// <summary>
/// 后台消费进程没有 HTTP 租户上下文，返回 null。
/// </summary>
public sealed class BackgroundCurrentTenantAccessor : ICurrentTenantAccessor
{
	public Guid? TenantId => null;
	public string? TenantCode => null;
}
