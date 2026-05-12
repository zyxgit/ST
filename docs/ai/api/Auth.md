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

## 禁止事项

- 禁止在日志中输出完整 JWT 或 `SigningKey`。
- 禁止新接口默认 `AllowAnonymous` 除非公开 API。

## AI 注意事项

- 新增需登录接口时 **删除** 或 **不要加** `AllowAnonymous` 在类级别；若类为匿名，在方法上单独 `[Authorize]`。
