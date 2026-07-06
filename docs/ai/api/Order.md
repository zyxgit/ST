# Order 服务

## 概述

Order 微服务负责订单的创建、查询和取消。订单创建时通过 Outbox 模式发布集成事件，供 Inventory、Payment 等服务消费，实现跨服务最终一致性。

## 项目位置

- Api 层：`Api/src/Microservices/Order/ST.MS.Order.Api/`
- Application 层：`Api/src/Microservices/Order/ST.MS.Order.Application/`
- Domain 层：`Api/src/Microservices/Order/ST.MS.Order.Domain/`
- Infra 层：`Api/src/Microservices/Order/ST.MS.Order.Infra/`

## 表结构

### orders

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| order_no | varchar(50) | 订单号（唯一） |
| user_id | uuid | 下单用户 ID |
| total_amount | decimal(18,2) | 订单总金额 |
| status | int | 订单状态（0=Pending, 1=InventoryFrozen, 2=Paid, 3=Canceled, 4=Failed） |
| saga_instance_id | uuid | 关联的 Saga 实例 ID |
| cancel_reason | varchar(500) | 取消原因 |
| create_by | uuid | 创建人 |
| create_time | timestamp | 创建时间 |

**索引**：
- `ix_orders_order_no` — 唯一索引
- `ix_orders_user_id` — 按用户查询
- `ix_orders_status` — 按状态查询

### order_items

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| order_id | uuid | 所属订单 ID（外键） |
| sku_id | uuid | SKU ID（预留 Inventory 集成） |
| product_name | varchar(200) | 商品名称 |
| quantity | int | 数量 |
| unit_price | decimal(18,2) | 单价 |

### saga_instances

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| business_id | uuid | 关联业务 ID（OrderId） |
| saga_type | varchar(100) | Saga 类型（"OrderSaga"） |
| current_step | varchar(200) | 当前步骤名 |
| status | int | Saga 状态（0=Started, 1=Running, 2=Completed, 3=Compensating, 4=Compensated, 5=Failed） |
| retry_count | int | 重试次数 |
| last_error | text | 最后错误信息 |
| create_by | uuid | 创建人 |
| create_time | timestamp | 创建时间 |

### saga_steps

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| saga_id | uuid | 所属 Saga 实例 ID（外键） |
| step_name | varchar(200) | 步骤名称 |
| status | varchar(50) | 步骤状态（Pending/Completed/Failed/Compensated） |
| request_json | jsonb | 请求负载 |
| response_json | jsonb | 响应负载 |
| compensation_event | varchar(500) | 补偿事件类型 |

## API 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/orders` | 创建订单 |
| GET | `/api/orders/{id}` | 查询订单详情 |
| POST | `/api/orders/{id}/cancel` | 取消订单 |

### 创建订单

```http
POST /api/orders
Content-Type: application/json

{
  "userId": "00000000-0000-0000-0000-000000000001",
  "items": [
    {
      "skuId": "00000000-0000-0000-0000-000000000001",
      "productName": "测试商品",
      "quantity": 2,
      "unitPrice": 99.99
    }
  ]
}
```

### 取消订单

```http
POST /api/orders/{id}/cancel
Content-Type: application/json

{
  "reason": "用户取消"
}
```

## 集成事件

### OrderCreatedIntegrationEvent

订单创建后通过 Outbox 发布。Inventory 服务消费此事件冻结库存。

```csharp
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    List<OrderItemData> Items) : IntegrationEvent;
```

### OrderCanceledIntegrationEvent

订单取消后通过 Outbox 发布。Inventory 服务消费此事件释放冻结库存。

```csharp
public sealed record OrderCanceledIntegrationEvent(
    Guid OrderId,
    string Reason) : IntegrationEvent;
```

## Saga 状态机

```
OrderCreated → InventoryFreezing → InventoryFrozen → Paying → Paid
                    ↓                    ↓              ↓
              InventoryFailed      PaymentFailed   PaymentTimeout
                    ↓                    ↓              ↓
                 Failed              Canceled        Canceled
```

## Gateway 路由

- `/api/orders/{**catch-all}` → `order-cluster` (http://localhost:5090)
- `/orders/{**catch-all}` → `order-cluster`
- `/docs/order/{**catch-all}` → `order-cluster`

## 延迟关单（超时自动取消）

`OrderTimeoutCheckService` 后台任务定期扫描超时未支付订单并自动取消。

### 配置

```json
{
  "OrderTimeout": {
    "Enabled": true,
    "CheckIntervalSeconds": 60,
    "PaymentTimeoutMinutes": 30,
    "BatchSize": 100
  }
}
```

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `Enabled` | `true` | 是否启用超时检查 |
| `CheckIntervalSeconds` | `60` | 检查间隔（秒） |
| `PaymentTimeoutMinutes` | `30` | 支付超时时间（分钟） |
| `BatchSize` | `100` | 每次批量处理的最大订单数 |

### 扫描条件

```sql
SELECT * FROM orders
WHERE status IN (0, 1)          -- Pending 或 InventoryFrozen
  AND create_time < now - 30min -- 超过支付时限
ORDER BY create_time
LIMIT 100
```

### 取消流程

```
OrderTimeoutCheckService 扫描超时订单
    → 调用 OrderService.CancelOrderAsync
    → 订单状态改为 Canceled（reason = "支付超时自动取消"）
    → Outbox 写入 OrderCanceledIntegrationEvent
    → Inventory 消费事件释放冻结库存
    → Saga 补偿
```

### 压测工具

并发下单压测脚本位于 `tools/load-tests/order-create.sh`。

```bash
# 用法：bash order-create.sh [并发数] [总请求数] [Gateway地址]
bash tools/load-tests/order-create.sh 50 200 http://localhost:25000
```

## 与后续服务的集成预留

| 集成点 | 说明 |
|--------|------|
| Inventory 服务 | 消费 `OrderCreatedIntegrationEvent` 冻结库存，消费 `OrderCanceledIntegrationEvent` 释放库存 |
| Payment 服务 | 消费 `OrderCreatedIntegrationEvent` 发起支付，发布 `PaymentSucceededIntegrationEvent` / `PaymentFailedIntegrationEvent` |
| Saga 编排 | `SagaInstance` + `SagaStep` 表已创建，后续实现 SagaOrchestrator 协调各步骤 |

## 可观测性指标

Order 服务注册了自定义 OpenTelemetry 指标（Meter: `ST.Order`），在 `OrderMetrics.cs` 中定义。

### 指标列表

| 指标名 | 类型 | 说明 |
|--------|------|------|
| `st_order_created_total` | Counter | 下单成功数 |
| `st_order_canceled_total` | Counter | 订单取消数 |
| `st_order_saga_compensated_total` | Counter | Saga 补偿次数 |
| `st_order_create_duration_ms` | Histogram | 下单耗时 (ms) |

### 埋点位置

| 方法/Handler | 指标 |
|-------------|------|
| `OrderService.CreateOrderAsync` | created + duration |
| `OrderService.CancelOrderAsync` | canceled |
| `PaymentFailedHandler` | saga.compensated |
| `InventoryFreezeFailedHandler` | saga.compensated |

### Grafana Dashboard

- **ST - 订单 Saga**：`deploy/grafana/provisioning/dashboards/st-order-saga.json`
- **ST - 全局总览**：`deploy/grafana/provisioning/dashboards/st-overview.json`

### 压测脚本

```bash
k6 run --env GATEWAY_URL=http://localhost:25000 tools/load-tests/order-create.k6.js
```

## 禁止事项

- 禁止绕过 Outbox 直接发布集成事件
- 禁止在无事务保证的情况下修改订单状态和写入 Outbox 消息
- 禁止在订单项中直接引用 Inventory 实体（通过 SkuId 关联）
