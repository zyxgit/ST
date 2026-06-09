# ST 模板项目演进路线图

本文用于指导后续 AI Agent 围绕 **高并发场景、跨服务事务、可靠消息、可观测性、工程化交付** 持续完善 ST 模板项目。目标不是简单堆功能，而是形成可运行、可压测、可演示、可复用的企业级微服务后台模板。实际派发给 Codex / Claude Code 时，请先按 [`AgentExecutionGuide.md`](./AgentExecutionGuide.md) 将阶段拆成小任务。

## 目标定位

ST 当前已经具备以下基础：

- YARP Gateway：统一路由、CORS、ForwardedHeaders、文档入口、基础限流。
- Identity：用户、角色、菜单、权限、JWT、RefreshToken、验证码、登录失败控制。
- OperationLog：操作日志 API、RabbitMQ 异步消费、独立 Consumer。
- FileUpload：文件上传、下载、公开文件、可扩展存储接口。
- Infrastructure：EF Core、PostgreSQL、Redis、RabbitMQ EventBus、后台任务、OpenTelemetry、Docker Compose。

后续演进要围绕以下技术能力建设：

| 能力方向 | 技术能力 |
|----------|------------|
| 高并发 | Redis Lua、分布式限流、异步削峰、热点库存、压测、限流降级 |
| 跨服务事务 | Saga、Outbox、Inbox、补偿事务、最终一致性、幂等消费 |
| 可靠消息 | RabbitMQ、手动 ACK、DLQ、重试、消息重放、消息状态表 |
| 文件中心 | 分片上传、断点续传、秒传、异步合并、签名 URL |
| 可观测性 | OpenTelemetry、TraceId、Metrics、Loki、Grafana、业务指标面板 |
| SaaS 模板 | 多租户、租户级配额、租户级限流、租户级缓存键空间 |

## AI 开发通用规则

后续 AI Agent 在执行本路线图任一阶段前，必须遵守：

1. 先阅读 `docs/ai/AI-RULES.md`、`docs/ai/common/DocumentationSync.md`，再按任务域阅读 `docs/ai/api/*`、`docs/ai/web/*`。
2. 修改功能时必须同步维护相关文档，至少检查：
   - `README.md`
   - `docs/architecture/README.md`
   - `docs/api/README.md`
   - `docs/database/README.md`
   - `docs/deploy/README.md`
   - `docs/ai/**`
3. 新增微服务时必须同步：
   - Aspire AppHost 编排。
   - Gateway `ReverseProxy` 路由。
   - Docker Compose 服务与环境变量。
   - 数据库迁移与种子数据。
   - OpenAPI / Scalar 文档入口。
4. 每个阶段至少提供：
   - 可运行 API。
   - 数据模型和迁移。
   - 关键集成测试或最小验证脚本。
   - README 或专题文档。
   - 如果涉及高并发，提供 k6 / bombardier / curl 并发验证脚本。
5. 不允许只写空壳接口。每个功能必须能从 Gateway 入口跑通核心链路。

## 第一阶段：订单 Saga 与可靠消息样板

### 目标

构建一个可演示跨服务最终一致性的订单样板，作为项目中最核心的“高并发 + 跨服务事务”案例。

### 建议新增服务

| 服务 | 建议路径 | 职责 |
|------|----------|------|
| Order | `Api/src/Microservices/Order/` | 创建订单、订单状态机、订单查询、取消订单 |
| Inventory | `Api/src/Microservices/Inventory/` | 商品库存、库存冻结、库存扣减、库存释放 |
| Payment | `Api/src/Microservices/Payment/` | 模拟支付、支付成功、支付失败、支付回调 |

> 可先做三个微服务；Coupon、Notification 等后续再扩展，避免第一阶段范围过大。

### 核心业务流程

1. 用户创建订单。
2. Order Service 创建 `Pending` 订单。
3. Order Service 写入 Outbox 消息：`OrderCreatedIntegrationEvent`。
4. Outbox Publisher 异步发布消息到 RabbitMQ。
5. Inventory Service 消费订单创建事件，冻结库存。
6. Inventory Service 发布 `InventoryFrozenIntegrationEvent` 或 `InventoryFreezeFailedIntegrationEvent`。
7. Payment Service 模拟支付。
8. 支付成功发布 `PaymentSucceededIntegrationEvent`。
9. Order Service 将订单改为 `Paid`。
10. Inventory Service 将冻结库存转为已售库存。
11. 支付超时或失败时，Order Service 取消订单，Inventory Service 释放冻结库存。

### 必做技术点

#### 1. Saga 状态表

建议新增 Saga 通用表或 Order 专属流程表：

```text
saga_instances
- id
- business_id
- saga_type
- current_step
- status
- retry_count
- last_error
- created_at_utc
- updated_at_utc

saga_steps
- id
- saga_id
- step_name
- status
- request_json
- response_json
- compensation_event
- created_at_utc
- updated_at_utc
```

Saga 状态至少包含：

- `Started`
- `InventoryFreezing`
- `InventoryFrozen`
- `Paying`
- `Paid`
- `Canceling`
- `Canceled`
- `Compensating`
- `Failed`

#### 2. Outbox / Inbox

新增可靠消息基础设施，建议放在 `Api/src/Infrastructures/ST.Infra.EventBus` 或单独 `ST.Infra.ReliableMessaging`。

```text
outbox_messages
- id
- aggregate_id
- event_type
- payload
- status
- retry_count
- next_retry_at_utc
- occurred_at_utc
- sent_at_utc
- error_message

inbox_messages
- id
- message_id
- consumer
- event_type
- processed_at_utc
```

要求：

- 业务数据与 Outbox 消息必须在同一个本地数据库事务内提交。
- Outbox Publisher 后台任务负责扫描待发送消息并投递 RabbitMQ。
- 消费端必须基于 `MessageId + Consumer` 做幂等去重。
- 消费失败时记录错误和重试次数。

#### 3. 库存防超卖

Inventory Service 至少提供两层防护：

- Redis Lua 原子预扣热点库存。
- PostgreSQL 乐观锁或条件更新兜底。

建议 Redis 键：

```text
inventory:sku:{skuId}:available
inventory:sku:{skuId}:frozen
inventory:order:{orderId}:freeze
```

#### 4. 延迟关单

至少实现一种自动取消未支付订单的方案：

- RabbitMQ TTL + DLX。
- Hangfire / 后台任务扫描。
- Outbox 定时消息。

第一版推荐使用后台任务扫描，稳定且容易调试；第二版再升级为 RabbitMQ 延迟消息。

### 推荐 API

```text
POST /api/orders
GET  /api/orders/{id}
POST /api/orders/{id}/cancel

POST /api/inventory/skus
POST /api/inventory/skus/{skuId}/stock/increase
GET  /api/inventory/skus/{skuId}/stock

POST /api/payments/mock/pay
POST /api/payments/mock/fail
```

### 验收标准

- 能通过 Gateway 完成创建订单、冻结库存、模拟支付、订单变为已支付。
- 支付失败或超时后，订单取消，库存释放。
- 重复投递同一消息不会重复扣库存或重复改订单。
- RabbitMQ 暂时不可用时，业务数据不丢失，Outbox 可恢复投递。
- 提供至少一个并发下单压测脚本。

### 阶段成果说明

> 基于 Saga 编排模式实现订单、库存、支付服务的最终一致性，设计 Outbox / Inbox 可靠消息机制，结合 Redis Lua 库存预扣与 RabbitMQ 异步削峰，解决高并发下单场景中的超卖、重复消费和跨服务事务一致性问题。

## 第二阶段：Gateway 分布式限流与权限缓存

### 目标

将 Gateway 从单机限流升级为可横向扩展的生产级 API Gateway，并降低权限校验路径的数据库压力。

### 必做功能

#### 1. Redis 分布式限流

当前 Gateway 已有进程内 Fixed Window 限流，第二阶段升级为 Redis Lua 分布式限流。

限流维度：

- IP。
- 用户 ID。
- 接口路径。
- HTTP Method。
- Auth 接口特殊桶。
- 租户 ID，预留。

建议配置：

```json
{
  "RateLimiting": {
    "Enabled": true,
    "Mode": "Redis",
    "WindowSeconds": 60,
    "Rules": [
      {
        "Name": "auth-login",
        "PathPrefix": "/api/identity/user/login",
        "PermitLimit": 10,
        "PartitionBy": "Ip"
      }
    ]
  }
}
```

#### 2. 权限缓存

为 Identity 增加权限缓存：

```text
auth:user:{userId}:permissions
auth:user:{userId}:menus
auth:role:{roleId}:permissions
```

要求：

- 登录或鉴权时优先读 Redis。
- 权限、角色、菜单变更时主动删除相关缓存。
- 可选：通过 RabbitMQ 发布 `PermissionChangedIntegrationEvent` 通知其他服务失效本地缓存。

#### 3. 登录安全增强

在现有验证码冷却、每日次数限制、登录失败计数基础上，继续补：

- IP + 邮箱组合限流。
- 滑动窗口失败次数统计。
- 登录风控日志。
- 管理后台查看锁定原因。

### 验收标准

- Gateway 多实例部署时，限流计数共享。
- 登录接口、验证码接口、文件上传接口可配置独立限流策略。
- 权限变更后，用户重新访问能获取最新权限。
- 提供限流压测脚本和 Redis 键说明。

### 阶段成果说明

> 将 API Gateway 限流从单机 Fixed Window 升级为 Redis Lua 分布式限流，支持按用户、IP、路径和接口类型配置策略；设计权限缓存与事件驱动失效机制，降低认证授权链路数据库压力。

## 第三阶段：文件中心高并发上传能力

### 目标

将 FileUpload 从普通上传服务升级为支持大文件、高并发、断点续传的文件中心。

### 必做功能

#### 1. 分片上传

新增接口：

```text
POST /api/files/multipart/init
POST /api/files/multipart/{uploadId}/chunks
POST /api/files/multipart/{uploadId}/complete
GET  /api/files/multipart/{uploadId}/status
POST /api/files/check-by-hash
```

建议表：

```text
file_upload_sessions
- id
- file_name
- file_hash
- file_size
- chunk_size
- total_chunks
- uploaded_chunks
- status
- created_by
- expires_at_utc

file_upload_chunks
- id
- upload_id
- chunk_index
- chunk_hash
- size
- storage_path
- created_at_utc
```

#### 2. 秒传与去重

- 客户端上传前先提交文件 Hash。
- 服务端存在相同 Hash 且文件有效时直接返回已有文件信息。
- 对公共文件和私有文件分别考虑权限边界。

#### 3. 断点续传

- Redis Bitmap 或 Set 记录已上传分片。
- `status` 接口返回缺失分片列表。
- 重复上传同一分片必须幂等。

#### 4. 异步合并

- 分片全部上传后进入 `Merging` 状态。
- 后台任务合并分片。
- 合并成功后写最终文件记录。
- 失败时可重试或清理临时分片。

#### 5. 签名下载 URL

- 私有文件生成短期有效下载 URL。
- 签名包含文件 ID、过期时间、用户 ID 或租户 ID。
- 防止直接暴露存储路径。

### 验收标准

- 大文件可以分片上传并合并成功。
- 上传中断后可查询已上传分片并续传。
- 相同文件二次上传可秒传。
- 重复上传同一分片不会产生脏数据。
- 提供大文件上传脚本或前端最小页面。

### 阶段成果说明

> 设计文件上传中心，支持大文件分片上传、断点续传、秒传和异步合并，基于 Redis Bitmap 维护分片状态，基于文件 Hash 实现去重，并通过签名 URL 控制私有文件访问。

## 第四阶段：OperationLog 高吞吐与可恢复消费

### 目标

将 OperationLog 打造成高吞吐审计日志中心，增强失败恢复、死信重放和批量写入能力。

### 必做功能

#### 1. 批量消费与批量落库

当前消费者可继续演进为：

- RabbitMQ prefetch 配置化。
- 内存缓冲队列。
- 每 N 条或每 T 秒批量写库。
- 批量失败时降级为单条写入定位毒丸消息。

#### 2. 死信队列

新增：

- `st.operationlog.dlx`
- `st.operationlog.dlq`
- 最大重试次数。
- 指数退避。
- 死信消息查询 API。
- 死信消息重放 API。

#### 3. 日志归档

- 近期日志留在 PostgreSQL。
- 历史日志归档到 MinIO / OSS。
- 后台任务按时间归档。
- 查询接口按时间范围路由到不同存储。

#### 4. 可观测性面板

Grafana 面板建议包含：

- 每分钟日志消费量。
- 消费失败数。
- DLQ 堆积量。
- 平均落库耗时。
- API 请求 TraceId 关联日志。

### 验收标准

- 高并发操作日志写入时不阻塞主业务请求。
- 消费失败消息进入 DLQ，不无限重入队。
- 可在管理端或 API 查询并重放死信消息。
- 提供批量消费吞吐测试结果。

### 阶段成果说明

> 优化审计日志链路，基于 RabbitMQ prefetch、批量缓冲、批量落库和死信队列提升日志吞吐，支持失败消息查询与重放，实现业务请求与审计落库解耦。

## 第五阶段：可观测性与压测体系

### 目标

让项目不只是“能跑”，还要能展示性能、问题定位和稳定性治理能力。

### 必做功能

#### 1. 业务指标

为核心链路增加指标：

- 下单成功数。
- 库存冻结失败数。
- 支付成功数。
- Saga 补偿次数。
- Outbox 待发送数量。
- Inbox 重复消息数量。
- 文件上传吞吐量。
- OperationLog 消费延迟。

#### 2. Trace 贯通

要求：

- Gateway 到下游服务透传 TraceId。
- 集成事件携带 TraceId / CorrelationId。
- OperationLog 记录 TraceId、SpanId。
- 日志、链路、业务数据可通过 TraceId 关联。

#### 3. 压测脚本

建议新增目录：

```text
tools/load-tests/
- order-create.k6.js
- gateway-rate-limit.k6.js
- file-multipart-upload.k6.js
- operationlog-producer.k6.js
```

#### 4. Grafana Dashboard

建议新增：

```text
deploy/grafana/dashboards/
- st-overview.json
- st-order-saga.json
- st-operationlog.json
- st-gateway.json
```

### 验收标准

- 能一键启动可观测性栈。
- 能通过压测脚本产生可观察的指标和日志。
- 文档说明如何查看 TraceId、日志和 Grafana 面板。

### 阶段成果说明

> 基于 OpenTelemetry、Loki、Grafana 构建微服务可观测性体系，贯通 Gateway、业务服务、RabbitMQ 消息和审计日志，并提供 k6 压测脚本验证限流、异步削峰和 Saga 补偿链路。

## 第六阶段：多租户 SaaS 能力

### 目标

将 ST 从后台模板升级为 SaaS 后台模板，支持租户级隔离、租户级配额和租户级治理。

### 必做功能

#### 1. Tenant Service

新增租户中心：

```text
tenants
- id
- code
- name
- status
- package_id
- expire_at_utc

tenant_users
- tenant_id
- user_id
- role
```

#### 2. 数据隔离

第一版建议使用共享数据库 + `TenantId`：

- 实体基类增加 `TenantId`，或通过接口标记租户实体。
- EF Core 全局查询过滤器。
- 后台任务和 Consumer 显式传递 TenantId。
- Redis 键统一包含租户维度。

#### 3. 租户级配额

- 用户数上限。
- 存储容量上限。
- API 调用次数上限。
- 文件上传大小上限。
- 订单 / 消息等业务资源上限。

#### 4. 租户级限流

Gateway 支持按租户限流：

```text
rate:{tenantId}:{route}:{window}
```

### 验收标准

- 不同租户数据互不可见。
- 租户禁用后无法访问业务 API。
- 租户配额超限时返回明确错误。
- Redis、日志、事件消息包含租户上下文。

### 阶段成果说明

> 实现多租户 SaaS 基础能力，基于 TenantId 全局数据过滤、租户级缓存键空间、租户级 API 限流和资源配额控制，支持后台管理系统 SaaS 化部署。

## 推荐实施顺序

| 优先级 | 阶段 | 原因 |
|--------|------|------|
| P0 | 第一阶段：订单 Saga 与可靠消息样板 | 最能体现跨服务事务和高并发业务深度 |
| P1 | 第二阶段：Gateway 分布式限流与权限缓存 | 强化生产级网关与认证授权性能 |
| P1 | 第三阶段：文件中心高并发上传能力 | 贴近真实业务，容易演示和压测 |
| P2 | 第四阶段：OperationLog 高吞吐与可恢复消费 | 强化异步系统可靠性 |
| P2 | 第五阶段：可观测性与压测体系 | 让技术亮点可证明、可展示 |
| P3 | 第六阶段：多租户 SaaS 能力 | 将模板升级为 SaaS 方向，范围较大，适合后置 |

## 每阶段完成后的统一交付清单

AI Agent 完成任一阶段后，必须输出并检查：

- [ ] 新增 / 修改的微服务、Controller、DTO、实体、迁移。
- [ ] Gateway 路由与限流配置。
- [ ] Aspire AppHost 编排。
- [ ] Docker Compose 编排。
- [ ] Redis 键空间文档。
- [ ] RabbitMQ Exchange / Queue / RoutingKey 文档。
- [ ] OpenAPI / Scalar 是否可访问。
- [ ] 单元测试、集成测试或最小验证脚本。
- [ ] 压测脚本和运行说明。
- [ ] README 与 `docs/ai/**` 文档同步。

## 推荐最终模板能力描述

完成前三阶段后，模板应具备以下能力：

> ST 是一个基于 .NET 微服务架构的后台管理模板，包含 YARP 网关、Identity 权限中心、FileUpload 文件中心、OperationLog 审计中心和订单 Saga 示例。项目使用 PostgreSQL、Redis、RabbitMQ、OpenTelemetry、Grafana 等技术，支持 Redis Lua 库存预扣、RabbitMQ 异步削峰、Outbox / Inbox 可靠消息、Saga 补偿事务、分布式限流、分片上传和可观测性压测体系。

