# database.skill

## 1. Skill Name

`st-database-postgres-ef` — PostgreSQL + EF Core 在 ST 中的用法与迁移纪律。

## 2. Purpose

- 约束 DbContext 注册、迁移位置、连接配置，避免 AI 生成不可部署或无双迁移的实体变更。

## 3. Tech Stack

| 项 | 事实 |
|----|------|
| 引擎 | PostgreSQL（Npgsql） |
| 抽象 | `ST.Infra.EntityFramework`、`ST.Infra.EntityFramework.Npgsql` |
| 注册 | `AddNpgsqlDbContextFromConfig<TContext>()` |
| 迁移 | 各服务 `*.Infra` 项目 |
| 启动 | `UseSharedWebApi` 末尾 `ExecuteCodeFirstExecutorsAsync()`（CodeFirst/种子流程） |

## 4. Architecture Rules

- 每微服务独立 `DbContext`（如 `AppDbContext`）；连接串来自配置 `Database:ConnectionString`（环境变量 `Database__ConnectionString`）。
- 实体与配置类放在 `*.Infra`；领域实体在 `*.Domain`。
- **禁止外键**：`EfDbContextBase.OnModelCreating` 调用 `ApplyNoForeignKeys()` 从模型层移除所有 FK 关系；`NoForeignKeySqlGenerator` 兜底拦截迁移 SQL 生成。双保险保证数据库不含 FOREIGN KEY 约束。

## 5. Coding Rules

- 变更模型后：**必须** `dotnet ef migrations add` 于 Infra 项目；审查 Up/Down。
- 查询：优先 LINQ；原生 SQL 必须参数化。
- UTC：`DateTime` 存储与 API 约定见 `docs/ai/api/Result.md`（ISO 8601 / UTC 偏好）。

## 6. Naming Rules

- DbContext：`<ContextName>DbContext`；迁移：`YYYYMMDDHHMMSS_Description`。
- 表名/列名：与现有 Fluent API / 约定一致；新增表勿与共享库冲突。

## 7. Best Practices

- 大数据量迁移：分批、索引 CONCURRENTLY（若手写 SQL）离线窗口。
- 软删：实体实现 `ISoftDelete` 等（以仓库现有接口为准）。

## 8. Forbidden Practices

- 仅改实体文件不添加迁移。
- 在生产迁移里无条件 `DROP` / `TRUNCATE` 业务表。
- 连接串、超级用户密码入库。

## 9. AI Generation Constraints

- 生成迁移命令必须含：`--project <Infra.csproj>`、`--startup-project <Api.csproj>`、`--context <DbContextName>`。
- 新增全局查询过滤器（多租户）须单独评审，不默认生成。

## 10. Example Code

```csharp
// InfraModule
services.AddNpgsqlDbContextFromConfig<AppDbContext>();
```

```bash
dotnet ef migrations add AddOrderTable \
  --project Api/src/Microservices/Test/ST.MS.Test.Infra/ST.MS.Test.Infra.csproj \
  --startup-project Api/src/Microservices/Test/ST.MS.Test.Api/ST.MS.Test.Api.csproj \
  --context AppDbContext
```

## 11. Related Documents

- `docs/ai/api/EFCore.md`、`PostgreSQL.md`、`Repository.md`
- `docs/database/README.md`
- `docs/ai/api/ServiceTemplate.md`
