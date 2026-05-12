# 数据与存储导航

## 当前技术栈

- **RDBMS**：PostgreSQL（EF Core Npgsql 提供方见 `ST.Infra.EntityFramework.Npgsql`）。
- **缓存**：Redis（`ST.Infra.Redis`，与 `AddSharedWebApi` 体系配套）。
- **ORM**：EF Core，各微服务 `*.Infra` 中定义 `DbContext` 与迁移。

## 规范真源

| 主题 | 文档 |
|------|------|
| DbContext、迁移、CodeFirst | [`../ai/api/EFCore.md`](../ai/api/EFCore.md) |
| PostgreSQL 连接与部署注意 | [`../ai/api/PostgreSQL.md`](../ai/api/PostgreSQL.md) |
| 仓储与聚合访问 | [`../ai/api/Repository.md`](../ai/api/Repository.md) |
| 缓存键、防击穿、与业务层边界 | [`../ai/common/Cache.md`](../ai/common/Cache.md) + [`../ai/api/Redis.md`](../ai/api/Redis.md) |
| 多租户数据隔离（预留） | [`../ai/common/MultiTenant.md`](../ai/common/MultiTenant.md) |

## 配置入口

- 连接解析：`Database:Provider`、`Database:ConnectionString`（及历史键名兼容）由共享配置与各服务 `appsettings` 提供；生产用环境变量覆盖。

## AI 注意

- 新增表/字段必须走 **EF 迁移** 与 Code Review，禁止仅改实体不生成迁移（见 `EFCore.md` 禁止项）。
