# Payment 服务

## 概述

Payment 微服务提供模拟支付能力。消费 `OrderCreatedIntegrationEvent` 创建待支付记录，通过手动 API 触发模拟支付成功/失败，发布回执事件让 Order 更新状态。

## 项目位置

- Api 层：`Api/src/Microservices/Payment/ST.MS.Payment.Api/`
- Application 层：`Api/src/Microservices/Payment/ST.MS.Payment.Application/`
- Domain 层：`Api/src/Microservices/Payment/ST.MS.Payment.Domain/`
- Infra 层：`Api/src/Microservices/Payment/ST.MS.Payment.Infra/`

## 表结构

### payments

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| order_id | uuid | 关联订单 ID |
| amount | decimal(18,2) | 支付金额 |
| status | int | 状态（0=Pending, 1=Succeeded, 2=Failed） |
| failure_reason | varchar(500) | 失败原因 |
| create_by | uuid | 创建人 |
| create_time | timestamp | 创建时间 |

## API 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/payments/mock/pay?orderId={id}` | 模拟支付成功 |
| POST | `/api/payments/mock/fail?orderId={id}&reason={reason}` | 模拟支付失败 |
| GET | `/api/payments/{orderId}` | 查询支付记录 |

## 完整 Saga 流程

```
1. 用户创建订单
   → Order Service: 创建 Pending 订单 + Outbox OrderCreated

2. Inventory 消费 OrderCreated
   → 库存充足: 冻结库存 + Outbox InventoryFrozen
   → 库存不足: Outbox InventoryFreezeFailed → Order 标记 Failed

3. Order 消费 InventoryFrozen
   → 更新订单状态为 InventoryFrozen

4. Payment 消费 OrderCreated
   → 创建待支付记录

5. 手动触发模拟支付
   POST /api/payments/mock/pay?orderId={id}
   → Payment Service: 标记支付成功 + Outbox PaymentSucceeded

6. Order 消费 PaymentSucceeded
   → 更新订单状态为 Paid
   → Saga Complete

=== 支付失败场景 ===

5'. 手动触发模拟支付失败
    POST /api/payments/mock/fail?orderId={id}&reason=余额不足
    → Payment Service: 标记支付失败 + Outbox PaymentFailed

6'. Order 消费 PaymentFailed
    → 取消订单 + Outbox OrderCanceled
    → Saga StartCompensation

7'. Inventory 消费 OrderCanceled
    → 释放冻结库存 + Outbox InventoryReleased
```

## Gateway 路由

- `/api/payments/{**catch-all}` → `payment-cluster` (http://localhost:5092)
- `/docs/payment/{**catch-all}` → `payment-cluster`

## 可观测性指标

Payment 服务注册了自定义 OpenTelemetry 指标（Meter: `ST.Payment`），在 `PaymentMetrics.cs` 中定义。

### 指标列表

| 指标名 | 类型 | 说明 |
|--------|------|------|
| `st_payment_succeeded_total` | Counter | 支付成功数 |
| `st_payment_failed_total` | Counter | 支付失败数 |

### 埋点位置

| 方法 | 指标 |
|------|------|
| `PaymentService.MockPayAsync` | succeeded |
| `PaymentService.MockFailAsync` | failed |

### Grafana Dashboard

- **ST - 订单 Saga**：`deploy/grafana/provisioning/dashboards/st-order-saga.json`
- **ST - 全局总览**：`deploy/grafana/provisioning/dashboards/st-overview.json`
