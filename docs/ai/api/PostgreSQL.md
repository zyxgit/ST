# PostgreSQL 规范

## 目录

- [事实](#事实)
- [连接配置](#连接配置)
- [SQL 与方言](#sql-与方言)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 事实

- ST 使用 **Npgsql** 作为 EF Core 提供程序（`ST.Infra.EntityFramework.Npgsql`）。
- 本地 Aspire 场景下 Postgres 常由 Aspire 资源注入（参见 `AppHost`）。

## 连接配置

与 `SERVICE_TEMPLATE` 一致，推荐使用环境变量覆盖：

```
Database__ConnectionString=Host=...;Port=5432;Database=...;Username=...;Password=...
Database__Provider=Npgsql
```

（具体键名以 `DatabaseConnectionInfo` 解析逻辑为准。）

## SQL 与方言

- 优先使用 **EF LINQ**；原生 SQL 使用 `ExecuteSqlRaw` 时参数化，禁止拼接用户输入。
- 利用 PostgreSQL：`jsonb`、`uuid`、部分索引等需在迁移中显式配置。

## 推荐方案

- 使用 **`uuid`** 或 **ULID** 作为主键策略前与团队对齐（现有实体以仓库为准）。
- 大表删除与归档：批量任务 + Hangfire（见 `Hangfire.md`）。

## 禁止事项

- 禁止在迁移中执行 **无条件的 TRUNCATE** 生产数据。
- 禁止超级用户连接串进入仓库。

## AI 注意事项

- 生成原始 SQL 时注明 **仅在 PostgreSQL 有效**，并在 CI 中覆盖集成测试。
