# 跨服务集成事件

## 概述

集成事件用于微服务之间的异步通信。各服务通过 Outbox 模式发布事件，消费端通过 Inbox 幂等处理。

集成事件定义集中在 `ST.Infra.IntegrationEvents` 项目中，各服务引用此项目获取事件定义。

## 项目位置

- 项目：`Api/src/Infrastructures/ST.Infra.IntegrationEvents/`
- 解决方案入口：`Api/src/ST.slnx`（02.Infrastructures 分组）

## 目录结构

```
ST.Infra.IntegrationEvents/
├── ST.Infra.IntegrationEvents.csproj
├── Orders/
│   ├── OrderCreatedIntegrationEvent.cs
│   ├── OrderCanceledIntegrationEvent.cs
│   └── OrderItemData.cs
├── Inventory/
│   ├── InventoryFrozenIntegrationEvent.cs
│   ├── InventoryFreezeFailedIntegrationEvent.cs
│   └── InventoryReleasedIntegrationEvent.cs
└── Payment/
    ├── PaymentSucceededIntegrationEvent.cs
    └── PaymentFailedIntegrationEvent.cs
```

## 事件列表

### Order 事件

| 事件 | 发布者 | 消费者 | 说明 |
|------|--------|--------|------|
| `OrderCreatedIntegrationEvent` | Order 服务 | Inventory | 订单创建，触发库存冻结 |
| `OrderCanceledIntegrationEvent` | Order 服务 | Inventory | 订单取消，触发库存释放 |

### Inventory 事件

| 事件 | 发布者 | 消费者 | 说明 |
|------|--------|--------|------|
| `InventoryFrozenIntegrationEvent` | Inventory | Order | 库存冻结成功，订单状态更新为 InventoryFrozen |
| `InventoryFreezeFailedIntegrationEvent` | Inventory | Order | 库存冻结失败（库存不足），订单标记 Failed |
| `InventoryReleasedIntegrationEvent` | Inventory | — | 库存已释放（订单取消场景） |

### Payment 事件

| 事件 | 发布者 | 消费者 | 说明 |
|------|--------|--------|------|
| `PaymentSucceededIntegrationEvent` | Payment | Order | 支付成功，订单状态更新为 Paid，Saga 完成 |
| `PaymentFailedIntegrationEvent` | Payment | Order | 支付失败，订单取消并触发库存释放 |

## 使用方式

### 发布事件（通过 Outbox）

```csharp
// 在业务服务中
var integrationEvent = new OrderCreatedIntegrationEvent(orderId, userId, totalAmount, items);
var outboxMessage = new OutboxMessage
{
    AggregateId = orderId,
    EventType = nameof(OrderCreatedIntegrationEvent),
    Payload = JsonSerializer.Serialize(integrationEvent),
    Status = OutboxStatus.Pending,
    OccurredAtUtc = DateTime.UtcNow
};

_dbContext.OutboxMessages.Add(outboxMessage);
await _dbContext.SaveChangesAsync(ct); // 与业务数据同一事务
```

### 消费事件（通过 Inbox 幂等）

```csharp
// 消费端 Handler
public class OrderCreatedHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken ct)
    {
        // 幂等检查
        if (await _inboxStore.ExistsAsync(@event.Id, "InventoryService", ct))
            return;

        // 处理业务逻辑（如冻结库存）
        await FreezeInventoryAsync(@event.Items, ct);

        // 标记已处理
        await _inboxStore.MarkAsProcessedAsync(@event.Id, "InventoryService", ct);
    }
}
```

## 链路追踪字段

`IntegrationEvent` 基类包含两个链路追踪字段，用于跨服务消息链路关联：

```csharp
public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>关联 ID，用于跨服务消息链路追踪</summary>
    public string? CorrelationId { get; init; }

    /// <summary>W3C TraceId，用于关联分布式链路</summary>
    public string? TraceId { get; init; }
}
```

### 自动填充

构造函数自动从 `Activity.Current` 提取 `TraceId` 和 `CorrelationId`。业务代码无需手动赋值：

```csharp
// TraceId 和 CorrelationId 自动从当前 Activity 填充
var integrationEvent = new OrderCreatedIntegrationEvent(orderId, userId, totalAmount, items);
// integrationEvent.TraceId == Activity.Current?.TraceId?.ToString()
```

### 传播链路

```
HTTP 请求 → Gateway (CorrelationId 中间件)
    │
    ▼
业务服务 (Activity.Current.TraceId)
    │
    ├──→ IntegrationEvent.TraceId → OutboxMessage.TraceId
    │
    ├──→ RabbitMqEventBus → BasicProperties.CorrelationId
    │
    └──→ 消费端 Activity 恢复 TraceContext
```

### RabbitMQ 消息属性

- 发布时：`IntegrationEvent.TraceId` 写入 `BasicProperties.CorrelationId`
- 消费时：从 `CorrelationId` 创建 Activity，恢复跨服务链路

## 禁止事项

- 禁止在事件中包含敏感信息（密码、Token 等）
- 禁止消费端直接修改发布端的数据（通过事件驱动，各自维护自己的状态）
- 禁止在无幂等检查的情况下消费事件
