# auth skill

## 适用场景

JWT、RefreshToken、权限策略、菜单权限、用户上下文、租户上下文。

## 必须先读

- `docs/backend/README.md`
- `docs/frontend/README.md`

## 常用源码路径

- `Api/src/Microservices/Identity/`
- `Api/src/ServiceShared/ST.Shared.WebApi/Authentication/`
- `Api/src/ServiceShared/ST.Shared.WebApi/Authorization/Permission.cs` — 权限常量 + `PermissionAuthorizeAttribute`
- `Api/src/ServiceShared/ST.Shared/Authentication/`
- `Web/src/constants/permissions.ts` — 前端权限常量
- `Web/src/router/`
- `Web/src/stores/`

## 开发规则

- 权限码采用 `<domain>:<resource>:<action>` 格式，如 `system:user:query`。
- **接口级权限**：使用 `[PermissionAuthorize(Permission.XXX)]` 特性，禁止手写 `"perm:xxx"` 字符串。
- **权限常量**：后端在 `Permission.cs` 中定义，前端在 `permissions.ts` 中定义，两端保持一致。
- **新增权限码**：需同步更新三处：
  1. `Api/src/Microservices/Identity/ST.MS.Identity.Api/Seeds/001_permissions.sql` — 种子数据
  2. `Api/src/Microservices/Identity/ST.MS.Identity.Infra/InfraModule.cs` — 管理员角色权限分配
  3. `Web/src/constants/permissions.ts` — 前端常量
- **自助接口**（当前用户修改密码/邮箱/头像等）：仅需 `[Authorize]`（基类已提供），不加权限策略。
- **公开接口**（登录/注册/刷新/文件公开下载）：使用 `[AllowAnonymous]`。
- 登录、刷新、登出必须考虑 token 生命周期和失效。
- 权限变更必须考虑缓存失效。
- 多租户操作必须保留租户上下文。

## 禁止事项

- 禁止提交 JWT SigningKey。
- 禁止日志输出完整 token、密码、验证码。
- 禁止前端只隐藏按钮而后端不鉴权。

## 不确定时必须询问

- 是否需要新增权限码？
- 权限属于菜单、按钮还是 API？
- 是否要租户级隔离？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `cd Web && pnpm build`
