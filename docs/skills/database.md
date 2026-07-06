# database skill

## 适用场景

EF Core、PostgreSQL、迁移、Redis、Outbox/Inbox、事务与并发控制。

## 必须先读

- `docs/database/README.md`
- `docs/backend/README.md`

## 常用源码路径

- `Api/src/Microservices/*/*.Infra/DbContext`
- `Api/src/Microservices/*/*.Infra/Migrations`
- `Api/src/Infrastructures/ST.Infra.ReliableMessaging/`
- `Api/src/Infrastructures/ST.Infra.Redis/`

## 开发规则

- 修改实体必须生成迁移。
- 高并发更新必须有原子性方案。
- Outbox 和业务数据必须同事务。
- Inbox 必须有幂等约束。

## 禁止事项

- 禁止服务间共享数据库写入。
- 禁止先查库存再普通更新库存。
- 禁止没有 TTL 或失效策略的缓存。

## 不确定时必须询问

- 数据是否按租户隔离？
- 是否需要唯一索引或幂等键？
- 是否要兼容历史数据迁移？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `git diff --check`
