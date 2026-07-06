# 架构导航（ST Monorepo）

## 总览

ST 采用 **单仓双应用**：`Api` 为 .NET 微服务与共享基础设施；`Web` 为 Vue3 管理端。长期目标：**SaaS 化、可水平扩展、服务边界清晰**。

## 后端（事实结构）

| 区域 | 路径 | 说明 |
|------|------|------|
| 解决方案 | `Api/src/ST.slnx` | 主解决方案（非根目录 sln） |
| Aspire | `Api/src/Aspire/` | 本地编排、服务发现、健康检查 |
| 微服务 | `Api/src/Microservices/*/` | 每服务 `*.Api` / `*.Application` / `*.Domain` / `*.Infra` |
| 共享 | `Api/src/ServiceShared/` | `ST.Shared.WebApi` 启动与中间件、`ST.Shared` 原语 |
| 基础设施 | `Api/src/Infrastructures/` | EF、Redis、EventBus、Tasks、ReliableMessaging 等 |
| 网关 | `Api/src/Microservices/Gateway/ST.Gateway` | YARP 反向代理、限流、文档入口；路由：`/api/files/*` → FileUpload、`/api/identity/*` → Identity、`/api/operationlog/*` → OperationLog |
| 文件上传 | `Api/src/Microservices/FileUpload/` | 文件上传与管理（本地存储，可扩展 MinIO/OSS） |

**依赖方向（约定）**：`*.Api` → `*.Application` → `*.Domain`；`*.Infra` 实现持久化与外部系统；跨服务复用见 `ST.Shared.*`。

## 前端（事实结构）

| 区域 | 路径 | 说明 |
|------|------|------|
| 入口 | `Web/src/main.ts` | 应用启动 |
| 路由 | `Web/src/router/` | 公共路由 + 管理端路由、权限 meta |
| 状态 | `Web/src/stores/` | Pinia，含 `auth` bootstrap 与菜单树 |
| 请求 | `Web/src/lib/request.ts` | Axios 基址、Bearer、401 刷新 |
| API | `Web/src/api/*.ts` | 按域拆分 API 调用 |

## 与 AI 规范的关系

- 分层、DTO、异常、EF、Redis、JWT 等**可执行规范**见 [`../ai/api/`](../ai/api/)。
- 前端路由、权限、请求、组件约定见 [`../ai/web/`](../ai/web/)。
- 通用 Git/Monorepo/多租户预留见 [`../ai/common/`](../ai/common/)。

## 深入阅读

- 架构总览与启动链详情见 [`docs/ai/common/Architecture.md`](../ai/common/Architecture.md) 与 [`docs/ai/api/ServiceTemplate.md`](../ai/api/ServiceTemplate.md)。
