# OperationLog 审计日志服务

## 目录

- [概述](#概述)
- [架构](#架构)
- [批量消费](#批量消费)
- [配置说明](#配置说明)
- [死信队列](#死信队列)
- [日志归档](#日志归档)
- [可观测性面板](#可观测性面板)
- [AI 注意事项](#ai-注意事项)

## 概述

OperationLog 是 ST 项目的审计日志服务，负责记录用户操作日志。核心特性：

- **异步写入**：通过 RabbitMQ 异步消费，不阻塞主业务请求
- **批量消费**：支持批量写库，提升高并发场景吞吐量
- **失败降级**：批量失败时降级为单条写入，定位毒丸消息

## 架构

```
业务服务 ──→ RabbitMQ ──→ OperationLog Consumer ──→ PostgreSQL
              │                    │
              │                    ├── 批量写库（默认）
              │                    └── 单条降级（批量失败时）
              │
              └── Exchange: st.operationlog
                  Queue: st.operationlog.consumer
```

### 项目结构

```
OperationLog/
├── ST.MS.OperationLog.Api/           # API 层（查询日志）
├── ST.MS.OperationLog.Application/   # 应用层（查询服务）
├── ST.MS.OperationLog.Consumer/      # RabbitMQ 消费者
│   ├── RabbitMqOperationLogConsumerHostedService.cs    # 单条消费者（兼容）
│   ├── BatchOperationLogConsumerHostedService.cs       # 批量消费者（推荐）
│   └── BufferedOperationLogEntry.cs                   # 缓冲区条目
└── ST.MS.OperationLog.Infra/         # 基础设施层
```

## 批量消费

### 工作原理

```
┌─────────────────────────────────────────────────────────────┐
│                   BatchOperationLogConsumerHostedService     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│   RabbitMQ ──→ 内存缓冲区 ──→ 批量写库 ──→ 批量 ACK        │
│                     │              │                        │
│                     │              └── 失败时降级为单条写入  │
│                     │                                        │
│                     └── 定时刷新（每 T 秒）                  │
│                         达到批量大小时立即刷新               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 触发条件

批量写库在以下任一条件满足时触发：

| 条件 | 配置项 | 默认值 | 说明 |
|------|--------|--------|------|
| 达到批量大小 | `BatchSize` | 50 | 缓冲区条数达到阈值 |
| 定时刷新 | `FlushIntervalSeconds` | 5 | 超过时间间隔 |
| 服务停止 | - | - | 优雅关闭时刷新剩余缓冲区 |

### 批量失败降级

```
批量写库（50 条）
    │
    ├── 成功 → 批量 ACK
    │
    └── 失败
         │
         └── FallbackToSingleOnBatchFailure = true
              │
              └── 逐条写库
                   ├── 成功 → 单条 ACK
                   └── 失败 → 单条 NACK + 记录日志
```

### 统计信息

消费者停止时输出统计：

```
Batch consumer stats: Received=10000, BatchWritten=9800, SingleWritten=150, Failed=50
```

| 指标 | 说明 |
|------|------|
| `Received` | 总接收消息数 |
| `BatchWritten` | 批量写入成功数 |
| `SingleWritten` | 单条降级写入成功数 |
| `Failed` | 最终失败数 |

## 配置说明

### appsettings.json

```json
{
  "RabbitMQ": {
    "OperationLog": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "ExchangeName": "st.operationlog",
      "RoutingKey": "operation_log",
      "QueueName": "st.operationlog.consumer",
      "PrefetchCount": 100,
      "Durable": true,
      "AutoDelete": false,
      
      "EnableBatchConsumer": true,
      "BatchSize": 50,
      "FlushIntervalSeconds": 5,
      "MaxRetryCount": 3,
      "FallbackToSingleOnBatchFailure": true
    }
  }
}
```

### 配置项说明

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `PrefetchCount` | ushort | 50 | RabbitMQ 预取数量 |
| `EnableBatchConsumer` | bool | true | 是否启用批量消费者 |
| `BatchSize` | int | 50 | 批量写库大小 |
| `FlushIntervalSeconds` | int | 5 | 定时刷新间隔（秒） |
| `MaxRetryCount` | int | 3 | 最大重试次数 |
| `FallbackToSingleOnBatchFailure` | bool | true | 批量失败时是否降级为单条写入 |
| `RequeueOnError` | bool | false | 消费失败是否重新入队 |

### 消费者模式切换

```json
{
  "EnableBatchConsumer": true   // 使用批量消费者（推荐）
  "EnableBatchConsumer": false  // 使用原有单条消费者（兼容）
}
```

## 性能对比

| 场景 | 单条消费 | 批量消费 |
|------|---------|---------|
| 1000 条/秒 | 1000 次 DB 写入 | 20 次 DB 写入（BatchSize=50） |
| 数据库连接 | 频繁获取/释放 | 复用同一连接 |
| 事务开销 | 1000 次事务 | 20 次事务 |
| 吞吐量 | ~500 条/秒 | ~2000 条/秒 |

## 死信队列

消费失败的消息保存到数据库死信表，支持查询和重放。

### 架构

```
消费失败（单条写入也失败）
    │
    ▼
┌─────────────────────────────────────┐
│  DeadLetterService                  │
│  - 保存原始消息（JSON）              │
│  - 记录错误信息和重试次数            │
│  - ACK 消息（不再重试）              │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  dead_letter_messages 表            │
│  - 支持查询和筛选                    │
│  - 支持手动重放                      │
└─────────────────────────────────────┘
```

### 数据模型

```sql
dead_letter_messages
├── id                    # 主键
├── original_message      # 原始消息（JSON）
├── queue_name            # 队列名称
├── exchange_name         # 交换机名称
├── routing_key           # 路由键
├── error_message         # 错误信息
├── error_stack_trace     # 错误堆栈
├── retry_count           # 已重试次数
├── max_retry_count       # 最大重试次数
├── message_created_at    # 消息创建时间
├── created_at            # 进入死信时间
├── replayed_at           # 重放时间
└── replay_result         # 重放结果
```

### 流程

```
1. 消息消费失败
      │
      ▼
2. 单条写入也失败
      │
      ▼
3. 发送到死信表（DeadLetterService）
      │
      ▼
4. ACK 消息（不再重试）
      │
      ▼
5. 管理员查询死信消息
      │
      ▼
6. 手动重放（重新发送到 RabbitMQ）
```

### 与 RabbitMQ 死信队列的区别

| 特性 | RabbitMQ DLQ | 数据库死信表 |
|------|-------------|-------------|
| 存储位置 | RabbitMQ | PostgreSQL |
| 查询能力 | 有限 | 支持分页、筛选 |
| 重放能力 | 需要额外代码 | 内置支持 |
| 持久化 | 依赖 RabbitMQ | 数据库持久化 |
| 适用场景 | 自动重试 | 手动排查和重放 |

> 当前实现使用数据库死信表，更适合审计和排查场景。后续可扩展为 RabbitMQ DLQ 实现自动重试。

### API 端点

| 方法 | 路由 | 说明 |
|------|------|------|
| `GET` | `/api/operationlog/dead-letters` | 查询死信消息（分页） |
| `GET` | `/api/operationlog/dead-letters/{id}` | 获取死信消息详情 |
| `POST` | `/api/operationlog/dead-letters/{id}/replay` | 重放单条死信消息 |
| `POST` | `/api/operationlog/dead-letters/batch-replay` | 批量重放死信消息 |

### 查询参数

| 参数 | 类型 | 说明 |
|------|------|------|
| `QueueName` | string | 队列名称筛选 |
| `IsReplayed` | bool | 是否已重放筛选 |
| `StartTime` | DateTime | 开始时间 |
| `EndTime` | DateTime | 结束时间 |
| `Page` | int | 页码（默认 1） |
| `PageSize` | int | 每页条数（默认 20） |

### 示例

```bash
# 查询未重放的死信消息
GET /api/operationlog/dead-letters?IsReplayed=false&Page=1&PageSize=20

# 获取详情
GET /api/operationlog/dead-letters/{id}

# 重放单条
POST /api/operationlog/dead-letters/{id}/replay

# 批量重放
POST /api/operationlog/dead-letters/batch-replay
{
  "ids": ["id1", "id2", "id3"]
}
```

## 日志归档

历史日志自动归档到文件系统或对象存储，减轻数据库压力。

### 归档策略

```
┌─────────────────────────────────────────────────────────────┐
│                    归档策略                                  │
├─────────────────────────────────────────────────────────────┤
│  时间范围          │  存储位置      │  说明                  │
├───────────────────┼───────────────┼───────────────────────┤
│  最近 30 天       │  PostgreSQL   │  热数据，频繁查询       │
│  30 天以前        │  文件系统      │  冷数据，归档存储       │
└─────────────────────────────────────────────────────────────┘
```

### 配置

```json
{
  "OperationLog": {
    "Archive": {
      "Enabled": true,
      "ArchiveAfterDays": 30,
      "BatchSize": 1000,
      "StorageType": "Local",
      "LocalArchivePath": "archives/operationlog",
      "FilePrefix": "operationlog",
      "DeleteAfterArchive": true,
      "ExecutionIntervalHours": 24
    }
  }
}
```

### 配置项说明

| 配置项 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `Enabled` | bool | false | 是否启用归档 |
| `ArchiveAfterDays` | int | 30 | 归档天数阈值 |
| `BatchSize` | int | 1000 | 每批归档数量 |
| `StorageType` | string | "Local" | 存储类型：Local/MinIO/OSS |
| `LocalArchivePath` | string | "archives/operationlog" | 本地归档路径 |
| `DeleteAfterArchive` | bool | true | 归档后是否删除源数据 |
| `ExecutionIntervalHours` | int | 24 | 归档任务执行间隔（小时） |

### 归档文件格式

归档文件为 JSON 格式，按日期目录组织：

```
archives/operationlog/
├── 2026/
│   ├── 05/
│   │   ├── 01/
│   │   │   ├── operationlog_20260501120000_abc123.json
│   │   │   └── operationlog_20260501180000_def456.json
│   │   └── 02/
│   │       └── operationlog_20260502120000_ghi789.json
│   └── 06/
│       └── ...
└── 2026/
    └── ...
```

### 归档查询

查询接口自动合并数据库和归档数据：

```
查询时间范围 > 30 天
    │
    ├── 数据库：查询最近 30 天
    │
    └── 归档文件：查询 30 天前的数据
         │
         └── 合并结果返回
```

### 归档流程

```
1. 后台任务每天执行一次
      │
      ▼
2. 查询超过 30 天的日志（最多 1000 条）
      │
      ▼
3. 序列化为 JSON 文件
      │
      ▼
4. 写入归档目录
      │
      ▼
5. 删除数据库中的已归档日志
      │
      ▼
6. 循环直到没有更多数据
```

### 存储类型

| 类型 | 说明 | 适用场景 |
|------|------|----------|
| `Local` | 本地文件系统 | 开发环境、单机部署 |
| `MinIO` | MinIO 对象存储 | 生产环境、私有云 |
| `OSS` | 阿里云 OSS | 生产环境、公有云 |

> 当前仅实现 Local 类型，MinIO 和 OSS 需要后续扩展。

## 可观测性面板

OperationLog Consumer 已接入 OpenTelemetry 自定义指标，通过 OTLP → Alloy → Prometheus → Grafana 链路实现可视化监控。

### 架构

```
OperationLog Consumer (OTel SDK)
    │
    ▼
Alloy (OTLP receiver)
    │
    ├── logs → Loki
    │
    └── metrics → Prometheus
                    │
                    ▼
              Grafana Dashboard
```

### 自定义指标

#### Meter 名称

`ST.OperationLog.Consumer`

#### 计数器 (Counter)

| 指标名 | 说明 |
|--------|------|
| `st_operationlog_messages_received_total` | 接收到的消息总数 |
| `st_operationlog_batch_write_success_total` | 批量写入成功数 |
| `st_operationlog_single_write_success_total` | 单条降级写入成功数 |
| `st_operationlog_write_failed_total` | 写入失败总数 |
| `st_operationlog_deadletter_written_total` | 写入死信表数 |
| `st_operationlog_deadletter_replay_success_total` | 死信重放成功数 |
| `st_operationlog_deadletter_replay_failed_total` | 死信重放失败数 |
| `st_operationlog_archive_count_total` | 归档日志条数 |
| `st_operationlog_archive_failed_total` | 归档失败次数 |

#### 直方图 (Histogram)

| 指标名 | 说明 |
|--------|------|
| `st_operationlog_batch_size` | 批量写入条数分布 |
| `st_operationlog_flush_duration_ms` | 刷新耗时 (ms) |

### Grafana Dashboard

Dashboard 名称：**ST - OperationLog Consumer**

自动加载路径：`deploy/grafana/dashboards/st-operationlog.json`

#### 面板列表

| 面板 | 类型 | 说明 |
|------|------|------|
| 消息接收速率（每分钟） | 时序图 | `rate(messages_received[1m]) * 60` |
| 批量写入成功数 | 统计 | 累计批量写入成功计数 |
| 单条降级写入 | 统计 | 累计单条降级写入计数（黄色 > 0，红色 > 100） |
| 写入失败总数 | 统计 | 累计写入失败（绿色 0，黄色 > 1，红色 > 10） |
| 死信写入数 | 统计 | 写入死信表的消息数 |
| 死信重放统计 | 统计 | 重放成功 / 重放失败 |
| 平均批量大小 | 时序图 | `rate(batch_size_sum) / rate(batch_size_count)` |
| 刷新耗时分布 | 时序图 | P50 / P95 / P99 分位数 |
| 归档统计 | 时序图 | 每小时归档条数 + 归档失败累计 |
| 消息处理速率 | 时序图 | 批量写入/s vs 单条降级/s vs 写入失败/s |

### Prometheus 查询示例

```promql
# 每分钟消息接收速率
rate(st_operationlog_messages_received_total[1m]) * 60

# 批量写入成功率
rate(st_operationlog_batch_write_success_total[5m])
/ (rate(st_operationlog_batch_write_success_total[5m]) + rate(st_operationlog_single_write_success_total[5m]) + rate(st_operationlog_write_failed_total[5m]))

# 刷新耗时 P95
histogram_quantile(0.95, rate(st_operationlog_flush_duration_ms_bucket[5m]))

# 平均批量大小
rate(st_operationlog_batch_size_sum[5m]) / rate(st_operationlog_batch_size_count[5m])
```

### 部署说明

1. 确保 `docker-compose.yml` 包含 Prometheus 服务
2. Alloy 配置中 metrics 管道已启用（指向 Prometheus remote_write）
3. Grafana 数据源已配置 Prometheus
4. Dashboard JSON 自动加载到 Grafana

## AI 注意事项

- 新增消费者配置时，同步更新 `RabbitMqOperationLogOptions` 和本文档
- 批量大小建议根据数据库性能调整，PostgreSQL 建议 50-100
- 刷新间隔建议 5-10 秒，过长会导致服务停止时丢失数据
- 生产环境建议 `RequeueOnError = false`，避免毒丸消息无限重试
