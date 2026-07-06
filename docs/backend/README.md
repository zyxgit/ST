# 后端开发规范

## 技术栈

- .NET / ASP.NET Core
- EF Core + PostgreSQL
- Redis / StackExchange.Redis
- RabbitMQ EventBus
- Outbox / Inbox ReliableMessaging
- YARP Gateway
- NLog + OpenTelemetry
- Aspire / Docker Compose

## 项目结构

后端解决方案入口：`Api/src/ST.slnx`。

新增业务服务时遵循：

```text
Api/src/Microservices/<Service>/
├── ST.MS.<Service>.Api
├── ST.MS.<Service>.Application
├── ST.MS.<Service>.Domain
└── ST.MS.<Service>.Infra
```

## 分层规则

| 层 | 可以做 | 禁止做 |
|----|--------|--------|
| Api | Controller、鉴权、请求绑定、返回 DTO | 写业务规则、直接操作 EF |
| Application | 应用服务、事务编排、DTO、事件处理 | 依赖 Controller、返回 EF IQueryable |
| Domain | 实体、值对象、枚举、领域方法 | 依赖 EF、Redis、HTTP、RabbitMQ |
| Infra | DbContext、迁移、Repository、外部服务实现 | 反向依赖 Api |

## Controller 与 API

- Controller 保持薄层，只负责协议转换和调用应用服务。
- 对外路径变更必须同步 Gateway、前端 API、文档。
- 统一使用共享异常中间件输出错误，不在 Controller 内拼接非标准错误格式。
- 需要鉴权的接口使用 JWT 和权限策略，权限命名采用 `perm:资源:动作` 风格。

## DTO 与 Result

- 入参和出参使用 DTO，禁止直接暴露 EF 实体。
- 分页接口使用统一分页 DTO 或当前仓库已有分页模型。
- 字段命名要与前端和 OpenAPI 保持一致。
- 业务异常使用项目已有业务异常类型，避免裸 `Exception` 表示可预期业务失败。

## EF Core 与数据库

- 每个服务维护自己的 DbContext 和迁移。
- 新增实体/字段必须生成迁移并更新数据库文档。
- 多表业务一致性优先使用本地事务；跨服务一致性使用消息/Saga，不使用分布式数据库事务。
- 高并发扣减类逻辑必须使用条件更新、乐观锁、Redis Lua 或等价原子机制。
- 禁止只改实体不生成迁移。

## Redis 与缓存

- 缓存键必须有命名空间，如 `identity:permissions:{userId}`、`inventory:sku:{skuId}:available`。
- 缓存必须定义 TTL 或明确说明长期有效和失效策略。
- 权限、租户、库存等关键缓存必须有主动失效或补偿机制。
- 库存预扣必须使用 Lua 或其他原子操作，不能用 `GET` 后 `SET` 的非原子流程。

## RabbitMQ 与可靠消息

- 发布跨服务业务事件优先写 Outbox，再由后台任务投递。
- 消费事件必须写 Inbox 幂等记录。
- 事件命名使用过去式业务事实，例如 `OrderCreatedIntegrationEvent`、`PaymentSucceededIntegrationEvent`。
- 事件 payload 应包含业务所需最小字段，禁止包含密码、完整 token、敏感密钥。

## 后台任务

- 长轮询任务使用 HostedService、现有 Tasks 基础设施或 Hangfire 风格能力。
- 任务必须支持配置开关、间隔、批量大小、重试或失败记录。
- 定时任务不得无界扫描大表，必须分页/批量处理。

## 日志与异常

- 日志必须包含足够上下文：业务 ID、用户/租户 ID（如可用）、traceId、事件类型。
- 禁止记录完整 JWT、RefreshToken、密码、验证码、生产连接串。
- 捕获异常后必须保留原始异常作为 inner exception 或日志异常对象。

## 新服务交付清单

- [ ] 四层项目创建并加入 `Api/src/ST.slnx`。
- [ ] Api 层接入共享启动、OpenAPI、认证、异常、日志。
- [ ] DbContext、实体、迁移、种子数据（如需要）。
- [ ] Gateway 路由和 DownstreamServices。
- [ ] Aspire AppHost 编排。
- [ ] Docker Compose 服务、环境变量、健康检查/依赖。
- [ ] 文档更新：README、architecture、backend/database/devops/status。
- [ ] 至少一个可执行验证命令。
