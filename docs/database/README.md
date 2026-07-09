# 数据库与存储文档

## 存储组件

| 组件 | 用途 |
|------|------|
| PostgreSQL | 各微服务主数据库 |
| EF Core | ORM、迁移、DbContext |
| Redis | 缓存、限流、库存 Lua 预扣 |
| RabbitMQ | 集成事件与异步消息 |
| Outbox/Inbox | 可靠消息状态与幂等消费 |

## 数据库边界

- 每个微服务拥有自己的 DbContext 和迁移。
- 服务间不得直接读写对方数据库表。
- 跨服务一致性使用集成事件、Outbox/Inbox、Saga、补偿事务。
- 业务表新增字段必须同步实体、配置、迁移、文档。

## 主要业务数据

| 服务 | 典型表 | 说明 |
|------|--------|------|
| Identity | users、roles、menus、tenants、tenant_users、tenant_quotas | 用户权限与租户 |
| OperationLog | operation_logs、dead_letters | 操作日志与死信 |
| FileUpload | files、multipart upload 相关表 | 文件元数据与分片上传 |
| Order | orders、order_items、saga_instances、saga_steps | 订单与 Saga 状态 |
| Inventory | skus、inventory_freeze_records | SKU 与冻结记录 |
| Payment | payments | 模拟支付记录 |
| ReliableMessaging | outbox_messages、inbox_messages | 可靠发布与幂等消费 |

## Outbox / Inbox 规则

`outbox_messages` 至少表达：消息 ID、聚合 ID、事件类型、payload、状态、重试次数、下一次重试时间、发生时间、发送时间、错误信息。

`inbox_messages` 至少表达：消息 ID、消费者、事件类型、处理时间。

约束：

- Outbox 与业务数据同事务提交。
- Publisher 只投递可重试消息，并更新状态。
- Inbox 使用 `MessageId + Consumer` 唯一约束防重复消费。

## Redis 键空间

建议键空间：

| 键 | 用途 |
|----|------|
| `inventory:sku:{skuId}:available` | 可用库存 |
| `inventory:sku:{skuId}:frozen` | 冻结库存 |
| `inventory:sku:{skuId}:sold` | 已售库存 |
| `gateway:rate-limit:{partition}:{path}` | 网关限流 |
| `identity:permissions:{userId}` | 用户权限缓存 |
| `tenant:{tenantId}:quota` | 租户配额缓存 |

## 迁移规则

- 新增/修改实体后必须生成 EF 迁移。
- 禁止手写生产 DDL 后不回写迁移。
- 迁移名称应描述业务目的，如 `AddTenantQuota`、`AddOrderSagaTables`。
- 迁移执行方式必须在发布说明或文档中说明。

## 高并发数据规则

- 库存扣减必须使用 Redis Lua、数据库条件更新或乐观锁。
- 不允许先查询库存再普通更新库存。
- 订单、支付、库存状态流转必须幂等。
- 唯一业务键必须建唯一索引或在应用层加幂等约束。
