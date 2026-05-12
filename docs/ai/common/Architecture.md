# 通用架构观（ST）

## 目录

- [总览](#总览)
- [Monorepo 边界](#monorepo-边界)
- [后端分层（概念）](#后端分层概念)
- [前端分层（概念）](#前端分层概念)
- [演进路线](#演进路线)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 总览

ST 目标：**SaaS 化**、**微服务演进**、**AI 辅助仍可读可运维**。架构陈述必须与仓库事实一致：

- 后端入口：`Api/src/ST.slnx`；编排：`Api/src/Aspire/`；网关：`Api/src/Microservices/Gateway/ST.Gateway`。
- 前端：`Web/` Vue3 + TypeScript + Vite。

## Monorepo 边界

| 目录 | 职责 |
|------|------|
| `Api/` | .NET 解决方案、微服务、共享库 |
| `Web/` | 管理端 SPA |
| `docs/` | 人与 AI 共用文档 |

网关（YARP）统一路由：`/api/identity/*` → Identity、`/api/operationlog/*` → OperationLog、`/api/test/*` → Test、`/api/files/*` → FileUpload。每新增微服务均需在网关 `appsettings.json` 与 `Program.cs` 中注册路由和集群。

**不推荐** 在 `Api` 内嵌前端源码或反向嵌入。

## 后端分层（概念）

```
*.Api → HTTP、OpenAPI、认证授权
*.Application → 应用服务、DTO、用例编排
*.Domain → 实体、领域规则（尽量少依赖基础设施）
*.Infra → DbContext、仓储实现、外部适配器
```

共享组件：`ST.Shared.WebApi`（管道模块）、`ST.Shared`、`ST.Shared.Application`、`ST.Shared.Domain`。

## 启动链

所有微服务入口 `Program.cs` 遵循同一管道（由 Aspire AppHost 统一编排启动）：

1. `AddServiceDefaults()` — 来自 `ST.Aspire.ServiceDefaults`，注册 OpenTelemetry（日志/指标/链路）、健康检查端点（`/health`、`/alive`）、服务发现与 HttpClient 弹性默认值。
2. `AddSharedWebApi(modules)` — 清空默认日志提供者并配置 NLog；切换 DI 容器为 Autofac 并扫描模块程序集；注册 Redis 与后台任务基础设施；调用每个模块的 `ConfigureServices`。
3. `UseSharedWebApi(modules)` — 注册全局异常中间件（ProblemDetails）、请求日志中间件；启用 HTTPS 重定向与授权；映射控制器；调用每个模块的 `Configure`。

模块通过 `ISharedModule` 接口（通常继承 `ServiceModule`）参与启动生命周期。

详细模板见 [ServiceTemplate.md](../api/ServiceTemplate.md)。

## Aspire 编排

本地开发通过 `Api/src/Aspire/ST.Aspire.AppHost` 启动，由 Aspire 自动编排所有微服务、中间件容器（Redis、PostgreSQL、RabbitMQ）和服务发现。

**注册新微服务**：

1. 在 `ST.Aspire.AppHost.csproj` 添加 `ProjectReference` 指向新服务的 `.Api.csproj`。
2. 在 `AppHost.cs` 调用 `builder.AddProject<Projects.ST_MS_YourService_Api>("name").WaitFor(...)`。

详细步骤见 [ServiceTemplate.md 的 Aspire 编排节](../api/ServiceTemplate.md#aspire-编排注册)。

## 前端分层（概念）

```
views / components → UI
stores (Pinia) → 会话状态、权限与菜单
api/* + lib/request.ts → HTTP 与错误处理
router → 路由与 meta.permission
types → TS 类型契约（与后端 DTO 对齐）
```

## 演进路线

- **短期**：单仓内清晰模块边界、网关聚合、共享库复用。
- **中期**：按域拆部署单元（已有微服务项目形态）、配置外部化、观测统一。
- **长期**：按租户与流量拆分集群；域事件与集成测试补齐。

## 推荐方案

- 新功能先落在 **清晰 bounded context**（微服务文件夹）内，避免跨服务共享数据库。
- 对外契约优先 **版本化 API** 与 **ProblemDetails** 错误模型。

## 禁止事项

- 禁止**绕过网关**在前端硬编码多个微服务根 URL（除非本地调试且文档说明）。
- 禁止在领域层直接依赖 `HttpClient`、EF `DbContext`（应通过应用服务或基础设施抽象）。

## AI 注意事项

- 生成新服务时，严格遵循 [ServiceTemplate.md](../api/ServiceTemplate.md) 中的 **Program.cs / InfraModule** 形态，并保持 `ISharedModule` 注册一致。
- 不确定依赖方向时，查阅本文件「启动链」节与 `docs/ai/api/Application.md`、`Domain.md`，或参考现有微服务（`Identity`、`Test`）。
