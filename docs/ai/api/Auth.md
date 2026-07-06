# 认证与授权（JWT）

## 目录

- [JWT 配置](#jwt-配置)
- [控制器](#控制器)
- [用户上下文](#用户上下文)
- [权限 Policy](#权限-policy)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## JWT 配置

```json
{
  "Jwt": {
    "Issuer": "st",
    "Audience": "st",
    "SigningKey": "put-a-strong-key-here",
    "AccessTokenMinutes": 120
  }
}
```

生产必须把 **`SigningKey`** 置于 UserSecrets 或环境变量（如 `Jwt__SigningKey`）。

## 控制器

- 基类 `AbstractControllerBase` 带 **`[Authorize]`**，匿名接口显式 **`[AllowAnonymous]`**。

```csharp
using Microsoft.AspNetCore.Authorization;
using ST.Shared.WebApi.Controller;

[AllowAnonymous]
public class TestController : AbstractControllerBase
```

## 用户上下文

注入 **`ST.Shared.Security.IUserContext`**：

```csharp
[Authorize]
[HttpGet("me")]
public IActionResult Me([FromServices] IUserContext userContext) => Ok(userContext.Email);
```

## 头像

- 头像文件通过 **FileUpload** 微服务上传（`POST /api/files/upload`，`accessLevel=Public`）。
- 用户实体存储 **`AvatarFileId`**（Guid?），关联 `FileUpload` 服务的 `FileEntity`。
- 前端通过 `/api/files/{avatarFileId}/public/download` 加载头像图片。
- 设置头像：`PUT /identity/api/user/users/{id}/avatar`（`{ "avatarFileId": "guid" }`）。
- 删除头像：`DELETE /identity/api/user/users/{id}/avatar`。
- `GET /identity/api/user/me` 返回 `avatarFileId` 字段。头像 URL 由前端拼接。**不加入 JWT Claims**（避免 token 体积膨胀）。

## 权限 Policy

- `Authorize(Policy = "perm:user:create")` — **perm:** 前缀约定。
- 角色：`Authorize(Roles = "admin")`。

## 推荐方案

- Access Token 短时 + Refresh 轮转（前端已实现刷新调用 `/identity/user/refresh`，路径以 Identity 服务为准）。
- 服务端校验 **`Audience`/`Issuer`**，时钟偏移配置合理值。

## 权限缓存

登录和刷新 Token 时，用户角色/权限会缓存到 Redis（`auth:user:{userId}:permissions`、`auth:user:{userId}:roles`），减少 DB 四表联查压力。

- **写入**：登录成功后、刷新 Token 缓存未命中时。
- **读取**：刷新 Token 时优先读缓存，命中则跳过 DB 关联查询。
- **失效**：用户角色变更或角色/权限变更时主动删除。TTL 与 Access Token 生命周期一致，作为兜底。
- **降级**：缓存读写失败时静默回退 DB，不影响主流程。

详见 [`Redis.md`](./Redis.md#权限缓存键空间)。

## 多租户登录

登录时可选指定 `tenant_code`，将用户绑定到特定租户：

```json
{
  "email": "user@example.com",
  "password": "****",
  "tenant_code": "acme"
}
```

- `tenant_code` 为可选字段，不填则不绑定租户（向后兼容）
- 验证租户存在且状态为 `Active`
- 验证用户属于该租户（`tenant_users` 表）
- JWT 写入 `tid`（租户 ID）和 `tcode`（租户编码）
- 权限缓存键自动包含租户维度：`t:{tid}:auth:user:{userId}:permissions`
- RefreshToken 持久化租户信息，刷新时自动恢复

详见 [`MultiTenant.md`](../common/MultiTenant.md)。

## 禁止事项

- 禁止在日志中输出完整 JWT 或 `SigningKey`。
- 禁止新接口默认 `AllowAnonymous` 除非公开 API。

## 登录安全增强

多维度登录失败限流，防暴力破解：

| 维度 | 窗口 | 阈值 | 超限行为 |
|------|------|------|----------|
| IP+邮箱 | 10 分钟 | 10 次 | 返回"请求过于频繁" |
| IP 总计 | 10 分钟 | 50 次 | 返回"IP 请求过于频繁" |
| 用户 | 30 分钟 | 5 次 | 账号锁定（需管理员解锁） |

**锁定原因追踪**：
- User 实体新增 `LockReason` 和 `LockedAtUtc` 字段。
- 登录失败超限 → `LockReason = "login_fail_exceeded"`。
- 管理员禁用 → `LockReason = "admin_disable"`。
- 管理员启用 → 清除 `LockReason` 和 `LockedAtUtc`。
- User 详情 API 返回 `lockReason` 和 `lockedAtUtc` 字段。

**迁移**：需执行 `dotnet ef migrations add AddLockReasonFields`。

## AI 注意事项

- 新增需登录接口时 **删除** 或 **不要加** `AllowAnonymous` 在类级别；若类为匿名，在方法上单独 `[Authorize]`。
