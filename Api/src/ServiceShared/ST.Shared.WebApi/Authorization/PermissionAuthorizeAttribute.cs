using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace ST.Shared.WebApi.Authorization;

/// <summary>
/// 权限授权特性，自动添加 "perm:" 前缀和 JWT Bearer 认证方案。
/// 使用方式：<c>[PermissionAuthorize(Permission.UserQuery)]</c>
/// </summary>
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
	public PermissionAuthorizeAttribute(string permission)
	{
		Policy = $"perm:{permission}";
		AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme;
	}
}
