# 命名规范（ST）

## 目录

- [C# / 后端](#c--后端)
- [TypeScript / 前端](#typescript--前端)
- [REST 与权限](#rest-与权限)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## C# / 后端

| 类型 | 约定 | 示例 |
|------|------|------|
| 程序集 | `ST.<Area>.<Name>` | `ST.MS.Identity.Api` |
| 接口 | `I` 前缀 | `IUserContext` |
| DTO（读） | `XxxDto` | `PagedResultDto<T>` |
| 应用服务 | `XxxService` | `TestService` |
| DbContext | `XxxDbContext` / 领域名 | `AppDbContext` |
| 模块 | `XxxModule` + `ISharedModule` | `InfraModule` |

**异步方法**：后缀 `Async`，返回 `Task` / `Task<T>` / `ValueTask`。

## TypeScript / 前端

| 类型 | 约定 | 示例 |
|------|------|------|
| 组件文件 | PascalCase | `AppLayout.vue` |
| composable | camelCase + `use` 前缀 | `useDiscrete`（Naive UI 封装） |
| Store | `useXxxStore` | `useAuthStore` |
| API 模块 | 领域名 | `api/auth.ts`、`api/menu.ts` |
| 类型 | PascalCase 接口 | `CurrentUser`、`MenuTreeNode` |

## REST 与权限

- Controller 路由：继承 `AbstractControllerBase` 时为 `api/[controller]`（见 `ST.Shared.WebApi`）。
- 权限 Policy：`perm:<resource>:<action>`，与 JWT 中权限声明对齐（见 `docs/ai/api/Auth.md`）。

## 推荐方案

- 新增微服务目录：`Microservices/<ServiceName>/ST.MS.<ServiceName>.*`，与现有 Identity/Test 一致。
- 前端路由 path：**小写 + 短横线**（与现有 `/dashboard`、`/login` 风格一致）。

## 禁止事项

- 禁止 `Svcs`、`Mgr`、`Helper2` 等模糊缩写作为公开类型名。
- 禁止前端 API 路径与后端实际 Controller 路由不一致（改路由必须双端同步）。

## AI 注意事项

- 生成 C# 类型时与现有 **命名空间层级** 对齐：`ST.MS.<Service>.Application`、`ST.MS.<Service>.Domain`。
- 生成 Vue 组件时 placed under `Web/src/components/` 或 `views/`，文件名 **PascalCase**。
