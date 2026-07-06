using ST.Shared;
using ST.Shared.Security;

namespace ST.Shared.WebApi.Authentication;

/// <summary>
/// 基于 HTTP 请求的租户上下文访问器，从 JWT claims 中读取租户信息。
/// 首次访问 TenantId 时同步设置 TenantContext.CurrentTenantId，确保 EF Core 查询过滤器生效。
/// </summary>
public sealed class HttpCurrentTenantAccessor : ICurrentTenantAccessor
{
	private readonly IUserContext _userContext;

	public HttpCurrentTenantAccessor(IUserContext userContext)
	{
		_userContext = userContext;
	}

	public Guid? TenantId
	{
		get
		{
			var tid = _userContext.TenantId;
			// 同步到 AsyncLocal，供 EF Core 查询过滤器和 SaveChanges 自动填充使用
			if (tid.HasValue && !TenantContext.CurrentTenantId.HasValue)
			{
				TenantContext.CurrentTenantId = tid;
			}
			return tid;
		}
	}

	public string? TenantCode => _userContext.TenantCode;
}
