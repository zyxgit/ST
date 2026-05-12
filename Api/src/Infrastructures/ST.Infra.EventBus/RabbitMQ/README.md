# ST.Infra.EventBus / RabbitMQ

RabbitMQ 版 EventBus（发布/订阅）。

## 配置（appsettings.json）

```json
{
  "RabbitMQ": {
    "EventBus": {
      "HostName": "localhost",
      "Port": 5672,
      "UserName": "guest",
      "Password": "guest",
      "VirtualHost": "/",
      "ExchangeName": "st.eventbus",
      "QueueName": "st.ms.test",
      "Durable": true,
      "AutoDelete": false,
      "PrefetchCount": 20,
      "PublishRetryCount": 3,
      "RequeueOnError": false
    }
  }
}
```

## 注册（DI）

```csharp
services.AddRabbitMqEventBus(Configuration);
```

## 订阅（启动时）

```csharp
eventBus.Subscribe<OrderCreatedEvent, OrderCreatedHandler>();
```

