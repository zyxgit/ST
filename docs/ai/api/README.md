# 后端（Api）AI 规范索引

本目录描述 **`Api/src`** 的真实架构与编码约束，与 `ST.Shared.*`、`ST.Infra.*`、`ST.MS.*` 代码一致。

## 文档列表

| 文档 | 主题 |
|------|------|
| [Application.md](./Application.md) | 应用层、模块、应用服务 |
| [Domain.md](./Domain.md) | 领域实体与异常语义 |
| [Repository.md](./Repository.md) | 仓储接口与实现放置 |
| [EFCore.md](./EFCore.md) | DbContext、迁移、UnitOfWork |
| [PostgreSQL.md](./PostgreSQL.md) | Npgsql、连接配置 |
| [Redis.md](./Redis.md) | 缓存客户端与用法边界 |
| [Auth.md](./Auth.md) | JWT、权限 Policy、IUserContext |
| [Gateway.md](./Gateway.md) | Gateway 限流与路由配置 |
| [Hangfire.md](./Hangfire.md) | 后台任务与调度 |
| [Upload.md](./Upload.md) | 文件上传演进约定 |
| [Logging.md](./Logging.md) | NLog、请求日志 |
| [Exception.md](./Exception.md) | BusinessException、ProblemDetails |
| [Result.md](./Result.md) | 分页与返回形态 |
| [DTO.md](./DTO.md) | DTO 放置与校验 |
| [ServiceTemplate.md](./ServiceTemplate.md) | 微服务模板（Program.cs / InfraModule） |
| [CodingStyle.md](./CodingStyle.md) | C# 风格与依赖注入 |
| [AI-Rules.md](./AI-Rules.md) | AI 生成约束清单 |
| [ReliableMessaging.md](./ReliableMessaging.md) | Outbox / Inbox 可靠消息基础设施 |
| [IntegrationEvents.md](./IntegrationEvents.md) | 跨服务集成事件定义 |
| [Order.md](./Order.md) | Order 订单服务（Saga、Outbox 集成） |
| [Inventory.md](./Inventory.md) | Inventory 库存服务（冻结/释放、事件消费） |
| [Payment.md](./Payment.md) | Payment 模拟支付服务（Saga 联动） |
| [OperationLog.md](./OperationLog.md) | OperationLog 审计日志服务（批量消费、死信队列） |
| [MultiTenant.md](../common/MultiTenant.md) | 多租户 SaaS 能力（租户隔离、配额、限流、JWT tid） |

## 必读路径

1. `docs/ai/common/Architecture.md`（架构总览与启动链）
2. `docs/ai/api/ServiceTemplate.md`（Program / InfraModule 模板）
3. 本文档目录中与任务相关的条目
