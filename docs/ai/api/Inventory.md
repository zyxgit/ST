# Inventory 服务

## 概述

Inventory 微服务负责 SKU 库存管理、库存冻结和库存释放。通过 RabbitMQ EventBus 消费 Order 服务的集成事件，实现跨服务库存联动。

## 项目位置

- Api 层：`Api/src/Microservices/Inventory/ST.MS.Inventory.Api/`
- Application 层：`Api/src/Microservices/Inventory/ST.MS.Inventory.Application/`
- Domain 层：`Api/src/Microservices/Inventory/ST.MS.Inventory.Domain/`
- Infra 层：`Api/src/Microservices/Inventory/ST.MS.Inventory.Infra/`

## 表结构

### skus

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| sku_id | uuid | SKU ID（唯一，与 Order 中的 SkuId 对应） |
| product_name | varchar(200) | 商品名称 |
| available | int | 可用库存 |
| frozen | int | 冻结库存（已下单未支付） |
| sold | int | 已售库存（已支付） |
| version | uint | 行版本号（乐观锁） |
| create_by | uuid | 创建人 |
| create_time | timestamp | 创建时间 |

### inventory_freeze_records

| 列名 | 类型 | 说明 |
|------|------|------|
| id | uuid | 主键 |
| order_id | uuid | 关联订单 ID |
| sku_id | uuid | SKU ID |
| quantity | int | 冻结数量 |
| status | int | 状态（0=Frozen, 1=Released, 2=Sold） |

**索引**：
- `ix_freeze_records_order_id_sku_id` — 唯一约束，防止重复冻结

## API 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/inventory/skus` | 创建 SKU |
| POST | `/api/inventory/skus/{skuId}/stock/increase` | 增加库存 |
| GET | `/api/inventory/skus/{skuId}/stock` | 查询库存 |

## 集成事件消费

### OrderCreatedIntegrationEvent → 冻结库存

```
Order Service 发布 OrderCreated
    → Inventory 消费
    → Inbox 幂等检查
    → DB 乐观锁冻结库存（UPDATE ... WHERE available >= quantity）
    → 成功：写入 Outbox InventoryFrozenIntegrationEvent
    → 失败：写入 Outbox InventoryFreezeFailedIntegrationEvent
    → 标记 Inbox 已处理
```

### OrderCanceledIntegrationEvent → 释放库存

```
Order Service 发布 OrderCanceled
    → Inventory 消费
    → Inbox 幂等检查
    → 释放冻结库存（frozen → available）
    → 写入 Outbox InventoryReleasedIntegrationEvent
    → 标记 Inbox 已处理
```

## 库存防超卖（双层防护）

### 第一层：Redis Lua 预扣（热点层）

通过 `IInventoryRedisService` 接口调用，基于 Lua 脚本实现原子操作：

```lua
-- TryFreezeAsync 的 Lua 脚本
local available = tonumber(redis.call('GET', KEYS[1]) or '0')
local quantity = tonumber(ARGV[1])
if available >= quantity then
    redis.call('DECRBY', KEYS[1], quantity)
    redis.call('INCRBY', KEYS[2], quantity)
    return 1
else
    return 0
end
```

- 返回 1：Redis 预扣成功，进入 DB 层
- 返回 0：Redis 库存不足，直接返回失败（不打 DB）

### 第二层：DB 乐观锁（兜底层）

```sql
UPDATE skus
SET available = available - @quantity,
    frozen = frozen + @quantity
WHERE sku_id = @skuId AND available >= @quantity
```

- 影响行数 = 1：冻结成功
- 影响行数 = 0：DB 库存不足，回滚 Redis 预扣，返回失败

### 完整流程

```
FreezeInventoryAsync:
  1. 幂等检查（同一订单已冻结则跳过）
  2. Redis Lua 预扣（逐项，失败则回滚已预扣项）
  3. DB 乐观锁（逐项，失败则回滚 Redis + 已冻结 DB 记录）
  4. 保存 FreezeRecords
```

### Redis 键空间

详见 [`Redis.md`](Redis.md#库存预扣键空间)。

```
inventory:sku:{skuId}:available   → String (可用库存)
inventory:sku:{skuId}:frozen      → String (冻结库存)
inventory:sku:{skuId}:sold        → String (已售库存)
```

## Gateway 路由

- `/api/inventory/{**catch-all}` → `inventory-cluster` (http://localhost:5091)
- `/docs/inventory/{**catch-all}` → `inventory-cluster`

## 可观测性指标

Inventory 服务注册了自定义 OpenTelemetry 指标（Meter: `ST.Inventory`），在 `InventoryMetrics.cs` 中定义。

### 指标列表

| 指标名 | 类型 | 说明 |
|--------|------|------|
| `st_inventory_freeze_success_total` | Counter | 库存冻结成功数 |
| `st_inventory_freeze_failed_total` | Counter | 库存冻结失败数（含超卖） |
| `st_inventory_released_total` | Counter | 库存释放数 |
| `st_inventory_freeze_duration_ms` | Histogram | 库存冻结耗时 (ms) |

### 埋点位置

| Handler | 指标 |
|---------|------|
| `OrderCreatedHandler.HandleAsync` | freeze.success / freeze.failed + duration |
| `OrderCanceledHandler.HandleAsync` | released |

### Grafana Dashboard

- **ST - 订单 Saga**：`deploy/grafana/provisioning/dashboards/st-order-saga.json`

## 禁止事项

- 禁止绕过 Outbox 直接发布集成事件
- 禁止在无幂等检查的情况下消费事件
- 禁止直接修改 `frozen` 字段而不通过冻结/释放流程
