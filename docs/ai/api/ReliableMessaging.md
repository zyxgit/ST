# 可靠消息基础设施（Outbox / Inbox）

## 概述

可靠消息基础设施用于保证跨服务消息的最终一致性。核心思想：

- **Outbox**：业务数据与集成事件在同一本地事务中写入 Outbox 表，由 Outbox Publisher 后台任务异步投递至 RabbitMQ，保证"不丢消息"。
- **Inbox**：消费端基于 `MessageId + Consumer` 做幂等去重，保证"不重复消费"。
- **Outbox Publisher**：后台服务周期性扫描可重试消息，通过 RabbitMQ 发布，支持指数退避重试。

## 项目位置

- 基础设施项目：`Api/src/Infrastructures/ST.Infra.ReliableMessaging/`
- 解决方案入口：`Api/src/ST.slnx`（02.Infrastructures 分组）

## 目录结构

```
ST.Infra.ReliableMessaging/
├── Abstractions/
│   ├── IOutboxStore.cs              # Outbox 存储接口
│   ├── IInboxStore.cs               # Inbox 存储接口
│   ├── IOutboxPublisher.cs          # 消息投递接口
│   ├── EfOutboxStore.cs             # EF Core 实现
│   ├── EfInboxStore.cs              # EF Core 实现
│   ├── RabbitMqOutboxPublisher.cs   # RabbitMQ 投递实现
│   ├── OutboxPublisherHostedService.cs  # 后台任务
│   ├── OutboxPublisherOptions.cs    # 配置模型
│   └── OutboxMetrics.cs            # 自定义 OpenTelemetry 指标
├── Configurations/
│   ├── OutboxMessageEntityTypeConfiguration.cs
│   ├── InboxMessageEntityTypeConfiguration.cs
│   └── ModelBuilderExtensions.cs   # modelBuilder.ApplyReliableMessaging()
├── DbContext/
│   └── ReliableMessagingDbContext.cs
├── Extensions/
│   └── ServiceCollectionExtensions.cs
├── Models/
│   ├── OutboxMessage.cs
│   ├── InboxMessage.cs
│   └── OutboxStatus.cs
├── GlobalUsings.cs
└── ST.Infra.ReliableMessaging.csproj
```

## 表结构

### outbox_messages

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| aggregate_id | uuid | 聚合根 ID，关联业务实体 |
| event_type | varchar(500) | 集成事件类型全名 |
| payload | jsonb | 序列化的事件负载 |
| status | int | 消息状态（0=Pending, 1=Sent, 2=Failed） |
| retry_count | int | 已重试次数 |
| next_retry_at_utc | timestamp | 下一次重试时间（指数退避） |
| occurred_at_utc | timestamp | 事件发生时间 |
| sent_at_utc | timestamp | 成功发送时间 |
| error_message | text | 最后一次错误信息 |
| trace_id | varchar(64) | W3C TraceId，创建时自动从 Activity.Current 提取 |

**索引**：
- `ix_outbox_messages_status` — 按状态查询
- `ix_outbox_messages_next_retry_at_utc` — 按重试时间查询
- `ix_outbox_messages_status_next_retry` — 复合索引，用于 Publisher 扫描

### inbox_messages

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| message_id | uuid | 消息 ID（来自 IntegrationEvent.Id） |
| consumer | varchar(300) | 消费者标识（服务名 + Handler 名） |
| event_type | varchar(500) | 集成事件类型全名 |
| received_at_utc | timestamp | 消息接收时间 |
| processed_at_utc | timestamp | 处理完成时间 |
| error_message | text | 处理失败时的错误信息 |
| retry_count | int | 已重试次数 |

**索引**：
- `ix_inbox_messages_message_id_consumer` — 唯一约束，保证幂等消费
- `ix_inbox_messages_processed_at_utc` — 按处理时间查询

## 使用方式

### 方式一：业务服务集成（推荐）

业务服务在自己的 DbContext 中添加 Outbox/Inbox DbSet，实现原子写入：

```csharp
// 在业务服务的 DbContext 中
public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyReliableMessaging(); // 注册实体配置
    base.OnModelCreating(modelBuilder);
}
```

### 方式二：独立 DbContext

使用 `ReliableMessagingDbContext` 作为独立的可靠消息存储，适用于 Outbox Publisher 后台任务扫描场景。

```csharp
// DI 注册
services.AddReliableMessaging(configuration);
```

### 注册 Outbox Publisher

```csharp
// 注册 Outbox Publisher 后台服务（读取 OutboxPublisher 配置节点）
services.AddOutboxPublisher(configuration);

// 或自定义配置
services.AddOutboxPublisher(options =>
{
    options.PollingIntervalSeconds = 5;
    options.BatchSize = 50;
    options.MaxRetryCount = 5;
    options.ExchangeName = "st.outbox";
});
```

### appsettings.json 配置示例

```json
{
  "OutboxPublisher": {
    "PollingIntervalSeconds": 5,
    "BatchSize": 50,
    "MaxRetryCount": 5,
    "BaseRetryDelaySeconds": 10,
    "ExchangeName": "st.outbox",
    "Durable": true,
    "ConnectionStringName": "rabbitmq"
  },
  "ConnectionStrings": {
    "rabbitmq": "amqp://guest:guest@localhost:5672/"
  }
}
```

## 接口说明

### IOutboxStore

| 方法 | 说明 |
|------|------|
| `Add(message)` | 添加一条 Outbox 消息（需随后 SaveChanges） |
| `AddRange(messages)` | 批量添加 |
| `GetPendingAsync(batchSize)` | 查询 Pending 状态的待发送消息 |
| `GetRetryableAsync(batchSize)` | 查询可重试消息（Pending 或 Failed 且已到达重试时间） |
| `MarkAsSentAsync(id)` | 标记已发送 |
| `MarkAsFailedAsync(id, error, nextRetry)` | 标记失败并设置重试时间 |

### IInboxStore

| 方法 | 说明 |
|------|------|
| `ExistsAsync(messageId, consumer)` | 检查消息是否已处理 |
| `Add(message)` | 记录 Inbox 消息 |
| `MarkAsProcessedAsync(messageId, consumer)` | 标记处理完成 |
| `MarkAsFailedAsync(messageId, consumer, error)` | 标记处理失败 |

### IOutboxPublisher

| 方法 | 说明 |
|------|------|
| `PublishAsync(message, ct)` | 将 Outbox 消息发布到 RabbitMQ |

### OutboxPublisherOptions

| 属性 | 默认值 | 说明 |
|------|--------|------|
| `PollingIntervalSeconds` | 5 | 轮询间隔（秒） |
| `BatchSize` | 50 | 每批拉取消息数量 |
| `MaxRetryCount` | 5 | 最大重试次数，超过不再自动重试 |
| `BaseRetryDelaySeconds` | 10 | 退避基数（秒），实际延迟 = Base × 2^RetryCount |
| `ExchangeName` | `st.outbox` | RabbitMQ Exchange 名称 |
| `Durable` | true | 消息是否持久化 |
| `ConnectionStringName` | `rabbitmq` | ConnectionStrings 中的键名 |

## Outbox Publisher 工作流程

```
┌─────────────────────────────────────────────────────┐
│               OutboxPublisherHostedService           │
│                                                      │
│  PeriodicTimer (每 N 秒)                             │
│       │                                              │
│       ▼                                              │
│  GetRetryableAsync(batchSize)                        │
│  ┌─ Pending 消息（首次发送）                          │
│  └─ Failed 消息且 NextRetryAtUtc <= now（可重试）     │
│       │                                              │
│       ▼                                              │
│  foreach message:                                    │
│    ├─ IOutboxPublisher.PublishAsync(message)          │
│    │    └─ RabbitMQ BasicPublish (EventType → routingKey)│
│    ├─ 成功 → MarkAsSentAsync                         │
│    └─ 失败 → MarkAsFailedAsync + 指数退避             │
│         ├─ 重试次数 < MaxRetry → 设置 NextRetryAtUtc  │
│         └─ 重试次数 >= MaxRetry → 永不重试            │
│       │                                              │
│       ▼                                              │
│  SaveChangesAsync（每条消息独立提交）                  │
└─────────────────────────────────────────────────────┘
```

## RabbitMQ 键空间

| Exchange | RoutingKey | 说明 |
|----------|------------|------|
| `st.outbox` | `{EventType}` | Outbox Publisher 使用事件类型全名作为路由键 |

> 消费端需创建队列并绑定到 `st.outbox` Exchange，使用 `EventType` 作为 routingKey。

## 重试策略

- **指数退避**：`delay = BaseRetryDelaySeconds × 2^RetryCount`
  - 第 1 次重试：10s 后
  - 第 2 次重试：20s 后
  - 第 3 次重试：40s 后
  - 第 4 次重试：80s 后
  - 第 5 次重试：不再重试（超过 MaxRetryCount）
- **最大重试次数**：默认 5 次，超过后消息保持 Failed 状态，需人工干预或 API 重放

## 与现有 EventBus 的关系

现有 `ST.Infra.EventBus` 提供直接的 RabbitMQ 发布/订阅能力。可靠消息基础设施在此基础上增加：

1. **事务性保证**：Outbox 消息与业务数据在同一事务中提交
2. **可靠投递**：Outbox Publisher 后台任务保证消息最终投递
3. **幂等消费**：Inbox 表防止重复处理
4. **失败恢复**：指数退避重试 + 最大重试次数限制

## 可观测性指标

Outbox 基础设施注册了自定义 OpenTelemetry 指标（Meter: `ST.Outbox`），在 `OutboxMetrics.cs` 中定义。

### 指标列表

| 指标名 | 类型 | 说明 |
|--------|------|------|
| `st_outbox_published_total` | Counter | Outbox 发布成功数 |
| `st_outbox_failed_total` | Counter | Outbox 发布失败数（超过最大重试次数） |
| `st_outbox_retried_total` | Counter | Outbox 重试数 |
| `st_outbox_publish_duration_ms` | Histogram | 发布耗时 (ms) |

### 埋点位置

| 组件 | 指标 |
|------|------|
| `OutboxPublisherHostedService.ProcessBatchAsync` | 发布成功 → published + duration，失败 → failed / retried |
| `RabbitMqOutboxPublisher.PublishAsync` | 发布耗时由 HostedService 计时 |

### Grafana 查询示例

```promql
# Outbox 发布成功率
rate(st_outbox_published_total[5m]) / (rate(st_outbox_published_total[5m]) + rate(st_outbox_failed_total[5m]))

# Outbox 发布耗时 P95
histogram_quantile(0.95, rate(st_outbox_publish_duration_ms_bucket[5m]))

# Outbox 重试速率
rate(st_outbox_retried_total[5m])
```

### Meter 注册

使用 Outbox 的服务需在 `Program.cs` 中注册 Meter：

```csharp
builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
{
    metrics.AddMeter("ST.Outbox");
});
```

## 禁止事项

- 禁止在 Outbox/Inbox 实体上添加业务逻辑
- 禁止绕过 Outbox 直接发布跨服务集成事件（应统一走 Outbox 模式）
- 禁止在无幂等检查的情况下消费 Inbox 消息

## AI 注意事项

- 新增集成事件时，序列化后的 Payload 存入 `outbox_messages.payload`（jsonb）
- 消费端必须先检查 `inbox_messages` 中 `MessageId + Consumer` 是否存在
- 重试逻辑使用 `next_retry_at_utc` 实现指数退避
- 表名、列名使用 snake_case（EF Core NamingConventions 自动转换）
- Outbox Publisher 使用 `GetRetryableAsync` 同时处理 Pending 和可重试的 Failed 消息
- 每条消息独立 SaveChanges，避免一条失败影响整批
