# 当前能力状态

本文记录仓库当前已实现的主要能力，避免路线图和历史流水账混杂。

## 已实现能力

| 能力 | 状态 | 说明 |
|------|------|------|
| Identity 用户权限 | 已实现 | 用户、角色、菜单、权限、登录、刷新、修改密码 |
| 多租户基础 | 已实现/预留 | 租户、租户用户、租户配额，业务表预留 tenant_id |
| OperationLog | 已实现 | API、异步消费、死信查询与重放（含前端管理页面） |
| FileUpload | 已实现 | 普通上传/下载、公开下载、签名 URL、分片上传、前端文件管理页面 |
| Gateway | 已实现 | YARP 路由、CORS、ForwardedHeaders、Redis 限流 |
| ReliableMessaging | 已实现 | Outbox/Inbox 模型、存储、Publisher 后台任务 |
| IntegrationEvents | 已实现 | Order、Inventory、Payment 集成事件 |
| Order | 已实现 | 创建、查询、取消、Saga 状态、超时关单 |
| Inventory | 已实现 | SKU、库存增加、冻结/释放、Redis Lua + DB 兜底 |
| Payment | 已实现 | 模拟支付成功/失败、支付记录查询 |
| 可观测性一期 | 已实现 | OTLP、Alloy、Loki、Prometheus、Grafana 配置 |
| 前端管理端 | 已实现 | 登录、用户、角色、菜单、操作日志、死信队列、文件管理、订单管理、库存管理、支付记录等管理能力 |

## 典型验证入口

| 目标 | 命令 |
|------|------|
| 后端构建 | `dotnet build Api/src/ST.slnx` |
| 前端构建 | `cd Web && pnpm build` |
| Aspire 启动 | `dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj` |
| Docker Compose | `cd deploy && docker compose up -d` |
| 文档空白/尾随检查 | `git diff --check` |

## 当前核心 API 摘要

| 服务 | API 摘要 |
|------|---------|
| Identity | 登录、刷新、登出、用户、角色、菜单、租户、配额 |
| FileUpload | `/api/files`、`/api/files/upload`、`/api/files/multipart/*` |
| OperationLog | `/api/operation-logs`、`/api/operationlog/dead-letters` |
| Order | `POST /api/orders`、`GET /api/orders/{id}`、`POST /api/orders/{id}/cancel` |
| Inventory | `POST /api/inventory/skus`、`POST /api/inventory/skus/{skuId}/stock/increase`、`GET /api/inventory/skus/{skuId}/stock` |
| Payment | `POST /api/payments/mock/pay`、`POST /api/payments/mock/fail`、`GET /api/payments/{orderId}` |

## 状态维护规则

- 已完成能力写在本文，未来计划写在 `docs/roadmap/README.md`。
- 新增能力时补充状态、验证入口和相关文档。
- 不在本文粘贴大段实现细节，细节进入对应架构/后端/数据库/部署文档。
