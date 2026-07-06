# 数据与存储导航

## 当前技术栈

- **RDBMS**：PostgreSQL（EF Core Npgsql 提供方见 `ST.Infra.EntityFramework.Npgsql`）。
- **缓存**：Redis（`ST.Infra.Redis`，与 `AddSharedWebApi` 体系配套）。
- **ORM**：EF Core，各微服务 `*.Infra` 中定义 `DbContext` 与迁移。
- **消息**：RabbitMQ（`ST.Infra.EventBus`），可靠消息表见 `ST.Infra.ReliableMessaging`。

## 规范真源

| 主题 | 文档 |
|------|------|
| DbContext、迁移、CodeFirst | [`../ai/api/EFCore.md`](../ai/api/EFCore.md) |
| PostgreSQL 连接与部署注意 | [`../ai/api/PostgreSQL.md`](../ai/api/PostgreSQL.md) |
| 仓储与聚合访问 | [`../ai/api/Repository.md`](../ai/api/Repository.md) |
| 缓存键、防击穿、与业务层边界 | [`../ai/common/Cache.md`](../ai/common/Cache.md) + [`../ai/api/Redis.md`](../ai/api/Redis.md) |
| 多租户数据隔离 | [`../ai/common/MultiTenant.md`](../ai/common/MultiTenant.md) |
| Outbox / Inbox 可靠消息表 | [`../ai/api/ReliableMessaging.md`](../ai/api/ReliableMessaging.md) |

## 配置入口

- 连接解析：`Database:Provider`、`Database:ConnectionString`（及历史键名兼容）由共享配置与各服务 `appsettings` 提供；生产用环境变量覆盖。

## 可靠消息表（Outbox / Inbox）

用于保证跨服务消息的最终一致性。表结构详见 [`../ai/api/ReliableMessaging.md`](../ai/api/ReliableMessaging.md)。

- `outbox_messages`：业务服务将集成事件写入此表，与业务数据在同一事务中提交。
- `inbox_messages`：消费端基于 `MessageId + Consumer` 做幂等去重。

基础设施项目：`Api/src/Infrastructures/ST.Infra.ReliableMessaging/`。

## Order 服务表

订单服务的数据库表。表结构详见 [`../ai/api/Order.md`](../ai/api/Order.md)。

- `orders`：订单主表（订单号、用户 ID、总金额、状态、Saga 实例 ID）。
- `order_items`：订单项表（SKU ID、商品名称、数量、单价）。
- `saga_instances`：Saga 实例表（业务 ID、Saga 类型、当前步骤、状态）。
- `saga_steps`：Saga 步骤表（步骤名、状态、请求/响应 JSON、补偿事件类型）。

数据库名：`st_order`。服务项目：`Api/src/Microservices/Order/`。

## Inventory 服务表

库存服务的数据库表。表结构详见 [`../ai/api/Inventory.md`](../ai/api/Inventory.md)。

- `skus`：SKU 库存主表（SKU ID、商品名称、可用库存、冻结库存、已售库存、乐观锁版本号）。
- `inventory_freeze_records`：库存冻结记录表（订单 ID、SKU ID、冻结数量、状态）。

数据库名：`st_inventory`。服务项目：`Api/src/Microservices/Inventory/`。

## Payment 服务表

支付服务的数据库表。表结构详见 [`../ai/api/Payment.md`](../ai/api/Payment.md)。

- `payments`：支付记录表（订单 ID、支付金额、状态、失败原因）。

数据库名：`st_payment`。服务项目：`Api/src/Microservices/Payment/`。

## AI 注意

- 新增表/字段必须走 **EF 迁移** 与 Code Review，禁止仅改实体不生成迁移（见 `EFCore.md` 禁止项）。

## Identity 服务表变更

User 实体新增字段（需执行迁移）：

| 字段 | 类型 | 说明 |
|------|------|------|
| `LockReason` | `string?` | 锁定原因（`"login_fail_exceeded"`、`"admin_disable"`） |
| `LockedAtUtc` | `DateTime?` | 锁定时间 |

迁移命令：`dotnet ef migrations add AddLockReasonFields --project Api/src/Microservices/Identity/ST.MS.Identity.Infra --startup-project Api/src/Microservices/Identity/ST.MS.Identity.Api`

## Identity 租户表

新增多租户支持相关表。表结构详见 [`../ai/common/MultiTenant.md`](../ai/common/MultiTenant.md)。

- `tenants`：租户主表（编码、名称、状态、套餐 ID、过期时间）。
- `tenant_users`：租户用户关联表（租户 ID、用户 ID、租户内角色）。复合主键 `(tenant_id, user_id)`。
- `tenant_quotas`：租户配额表（用户上限、存储上限、API 调用上限、文件大小上限、订单上限）。`tenant_id` 唯一索引。

数据库名：`st_identity`。服务项目：`Api/src/Microservices/Identity/`。

## 业务表租户字段

以下业务表新增 `tenant_id` 字段（需执行迁移）：

| 表 | 服务 | 说明 |
|----|------|------|
| `orders` | Order | 订单 |
| `skus` | Inventory | SKU 库存 |
| `payments` | Payment | 支付记录 |
| `files` | FileUpload | 文件记录 |
| `operation_logs` | OperationLog | 操作日志 |

EF Core 全局查询过滤器自动按 `tenant_id` 过滤，无需手动添加 WHERE 条件。
