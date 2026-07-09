# AI Skills

Skills 是给 AI Agent 的高密度执行卡，不替代完整文档。完整背景见 `docs/architecture`、`docs/backend`、`docs/frontend`、`docs/devops`、`docs/database`。

## 使用顺序

1. 先读 `docs/ai/README.md`。
2. 按任务选择一个主 skill。
3. 需要横切能力时叠加 auth/cache/database/logging/upload。
4. 不确定时先问用户。

## Skill 索引

| Skill | 文件 | 场景 |
|-------|------|------|
| Architecture | [`architecture.md`](./architecture.md) | 服务边界、Gateway、Aspire、消息、部署拓扑 |
| Backend | [`backend.md`](./backend.md) | .NET 微服务、Controller、Application、Domain、Infra、防 404/502 |
| Frontend | [`frontend.md`](./frontend.md) | Vue、Router、Pinia、Axios、组件 |
| Database | [`database.md`](./database.md) | EF Core、迁移、事务、Redis、Outbox/Inbox |
| Auth | [`auth.md`](./auth.md) | JWT、权限、用户上下文、租户 |
| Cache | [`cache.md`](./cache.md) | Redis 键、TTL、限流、Lua |
| Logging | [`logging.md`](./logging.md) | NLog、OpenTelemetry、traceId、脱敏 |
| Upload | [`upload.md`](./upload.md) | 文件上传、签名 URL、分片上传 |
| Prompt | [`prompt.md`](./prompt.md) | 需求审查、任务拆分、二次确认 |

## 通用 Skill 模板

每个 skill 应包含：适用场景、必须先读、常用源码路径、开发规则、禁止事项、不确定时必须询问的问题、验收检查。
