# 架构文档

## 总览

ST 是一个微服务后台管理系统模板，后端以 .NET 微服务拆分业务边界，前端以 Vue 3 构建管理端，基础设施通过 PostgreSQL、Redis、RabbitMQ、OpenTelemetry 和 Docker Compose/Aspire 支撑本地开发与部署。

```text
Web(Vue3) → ST.Gateway(YARP) → Identity / OperationLog / FileUpload / Test / Order / Inventory / Payment
                                      │
                                      ├─ PostgreSQL：各服务独立数据库/DbContext
                                      ├─ Redis：缓存、限流、库存 Lua 预扣
                                      ├─ RabbitMQ：集成事件、操作日志、可靠消息投递
                                      └─ OpenTelemetry → Alloy → Loki/Prometheus/Grafana
```

## 服务边界

| 服务 | 职责 | 主要路径 |
|------|------|----------|
| Gateway | 统一入口、YARP 路由、CORS、ForwardedHeaders、限流、文档跳转 | `Api/src/Microservices/Gateway/ST.Gateway` |
| Identity | 用户、角色、菜单、权限、认证、租户、配额 | `Api/src/Microservices/Identity` |
| OperationLog | 操作日志查询、异步消费、死信查询与重放 | `Api/src/Microservices/OperationLog` |
| FileUpload | 文件上传、下载、签名 URL、分片上传 | `Api/src/Microservices/FileUpload` |
| Test | 示例接口、基础设施验证、可靠消息测试 | `Api/src/Microservices/Test` |
| Order | 订单、订单项、Saga 状态、延迟关单 | `Api/src/Microservices/Order` |
| Inventory | SKU、库存冻结/释放/售出、Redis Lua + DB 乐观锁 | `Api/src/Microservices/Inventory` |
| Payment | 模拟支付成功/失败、支付记录 | `Api/src/Microservices/Payment` |

## 后端分层

业务微服务通常采用四层结构：

```text
ST.MS.<Service>.Api          # Controller、启动配置、OpenAPI
ST.MS.<Service>.Application  # DTO、应用服务、事件处理器
ST.MS.<Service>.Domain       # 实体、枚举、领域规则
ST.MS.<Service>.Infra        # DbContext、迁移、外部基础设施实现
```

共享能力放在：

| 目录 | 职责 |
|------|------|
| `Api/src/ServiceShared/ST.Shared` | 公共常量、异常、安全、模块化抽象 |
| `Api/src/ServiceShared/ST.Shared.WebApi` | 共享 WebAPI 启动、认证、异常中间件、OpenAPI、操作日志 |
| `Api/src/Infrastructures/ST.Infra.*` | EF、Redis、EventBus、ReliableMessaging、Repository、Tasks、Email |

## 网关路由

当前 `ST.Gateway` 通过 `ReverseProxy` 配置转发：

| 路由 | 服务 |
|------|------|
| `/api/identity/{**catch-all}`、`/identity/{**catch-all}` | Identity |
| `/api/operationlog/{**catch-all}`、`/operationlog/{**catch-all}` | OperationLog |
| `/api/test/{**catch-all}`、`/test/{**catch-all}` | Test |
| `/api/files/{**catch-all}` | FileUpload |
| `/api/orders/{**catch-all}`、`/orders/{**catch-all}` | Order |
| `/api/inventory/{**catch-all}` | Inventory |
| `/api/payments/{**catch-all}` | Payment |
| `/docs/{service}/{**catch-all}` | 对应服务文档 |

新增服务必须同步：Gateway `ReverseProxy`、`DownstreamServices`、Aspire AppHost、Docker Compose、文档和必要的前端代理配置。

## 跨服务事务与消息

订单样板使用最终一致性：

1. Order 创建订单和 Outbox 消息，二者在本地事务中提交。
2. Outbox Publisher 扫描待投递消息并发布到 RabbitMQ。
3. Inventory 消费 `OrderCreated`，执行 Redis Lua 预扣和数据库乐观锁冻结。
4. Payment 消费订单事件创建待支付记录。
5. 支付成功/失败事件回到 Order，驱动订单状态和 Saga 状态变化。
6. 取消订单事件驱动 Inventory 释放冻结库存。

关键约束：

- 业务数据与 Outbox 必须同事务提交。
- 消费端必须基于 `MessageId + Consumer` 做幂等。
- 消息处理失败必须记录错误和重试策略。
- 不允许跨服务直接共享 DbContext 或直接写其他服务数据库。

## 可观测性

- 日志：NLog + OpenTelemetry Logs。
- Metrics/Tracing：OpenTelemetry 预留并在共享启动中按环境变量启用。
- 本地栈：Grafana Alloy、Loki、Prometheus、Grafana。
- 追踪字段：日志、异常响应和跨服务调用应保留 `traceId` 或等价关联 ID。

## 架构禁止事项

- 禁止一个微服务直接访问另一个微服务的数据库表。
- 禁止把业务逻辑写入 Controller。
- 禁止绕过 Gateway 暴露新生产入口而不更新部署和文档。
- 禁止新增服务只建空壳，不接入运行、路由、配置和验证说明。
- 禁止将真实生产密钥写入 appsettings、Docker Compose 或文档示例。
