# auth.skill

## 1. Skill Name

`st-auth-jwt` — JWT、授权策略与前后端权限对齐。

## 2. Purpose

- 固定 `JwtBearer`、`perm:` Policy、`IUserContext`、前端 `PermissionCode` 与刷新路径，避免 AI 生成不一致的鉴权代码。

## 3. Tech Stack

| 项 | 事实 |
|----|------|
| 协议 | Bearer JWT |
| 控制器基类 | `AbstractControllerBase`，默认 `[Authorize]` |
| 策略 | `[Authorize(Policy = "perm:resource:action")]` |
| 上下文 | `ST.Shared.Security.IUserContext` |
| 前端 | `PermissionCode` + 路由 `meta.permission` + `authStore.hasPermission` |
| 刷新 | Axios `POST` `/identity/user/refresh`，header `x-refresh-retry` 防环 |

## 4. Architecture Rules

- 配置节：`Jwt:Issuer`、`Jwt:Audience`、`Jwt:SigningKey`、`Jwt:AccessTokenMinutes`（见 `docs/ai/api/Auth.md`）。
- 密钥：**仅** UserSecrets / 环境变量（如 `Jwt__SigningKey`）。
- 网关：鉴权通常在下游微服务完成；网关侧重 TLS、限流、路由。

## 5. Coding Rules

- 匿名接口：方法或类级 `[AllowAnonymous]`。
- 权限：`perm:` 前缀与前端 `system:xxx:xxx` 命名需在变更时 **双端对齐**。
- `IUserContext`：`UserId`、`Email`、`Roles`、`Permissions`（见共享接口注释）。

## 6. Naming Rules

- Policy 字符串：`perm:<模块>:<动作>`（后端）；前端常量：`PermissionCode` PascalCase 键 → `system:...` 值。

## 7. Best Practices

- Access 短时 + Refresh 轮转；401 仅触发一次刷新队列（见 `request.ts`）。
- 敏感操作二次校验（密码、2FA）在产品层扩展，不单靠前端隐藏按钮。

## 8. Forbidden Practices

- 日志打印完整 JWT、`SigningKey`。
- 新建业务 Controller 默认 `AllowAnonymous`。
- 前端仅用路由隐藏代表「已授权 API」。

## 9. AI Generation Constraints

- 新增 `PermissionCode` 必须同时提及后端 `Authorize(Policy=...)` 或等价注册处。
- 不修改 `AbstractControllerBase` 默认授权语义除非显式任务。

## 10. Example Code

```csharp
[Authorize(Policy = "perm:user:create")]
[HttpPost]
public IActionResult Create([FromServices] IUserContext ctx)
{
	return Ok(ctx.UserId);
}
```

```typescript
// constants/permissions.ts
export const PermissionCode = {
  UserCreate: 'system:user:create',
} as const
```

```typescript
// router guard — meta.permission
if (requiredPermission && !authStore.hasPermission(requiredPermission))
  return '/dashboard'
```

## 11. Related Documents

- `docs/ai/api/Auth.md`
- `docs/ai/web/Permission.md`、`Request.md`
