# EF Core 规范

## 目录

- [项目结构](#项目结构)
- [DbContext 注册](#dbcontext-注册)
- [迁移与 CodeFirst](#迁移与-codefirst)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 项目结构

- 核心抽象：`ST.Infra.EntityFramework`
- Npgsql 提供方：`ST.Infra.EntityFramework.Npgsql`（`AddNpgsqlDbContextFromConfig<TContext>()`）
- 各服务 `AppDbContext` 位于 `*.Infra` 项目。

## DbContext 注册

`InfraModule` 模板（与 `docs/ai/api/ServiceTemplate.md` 一致）：

```csharp
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.Shared.Module;

namespace ST.MS.Test.Infra;

public sealed class InfraModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);
		services.AddNpgsqlDbContextFromConfig<AppDbContext>();
	}
}
```

## 迁移与 CodeFirst

- 启动时 `UseSharedWebApi` 末尾会执行 `ExecuteCodeFirstExecutorsAsync()`（见 `ST.Shared.WebApi` 扩展），与 CodeFirst/种子机制配合。
- 新迁移在 **`*.Infra` 项目** 内通过 `dotnet ef migrations add` 生成，提交到仓库。

## 代码示例

设计时工厂（真实仓库中存在 `*DesignTimeDbContextFactory` 模式）用于 EF Tools：

```csharp
// 路径示例：ST.MS.Test.Infra/TestDesignTimeDbContextFactory.cs
```

（具体类名以各服务为准。）

## 推荐方案

- 连接串来自 **`Database:ConnectionString`**，勿硬编码。
- 审计字段由 **`AuditSaveChangesInterceptor`** 等拦截器维护（见 `ST.Infra.EntityFramework`）。

## 禁止事项

- 禁止在无迁移情况下修改生产库结构。
- 禁止在领域实体上使用 **`[NotMapped]`** 逃避迁移却不文档化例外。

## AI 注意事项

- 生成迁移命令时需指定 **startup 项目**（通常为对应 `*.Api`）与 **context**：

```bash
dotnet ef migrations add InitFeature --project Api/src/Microservices/Test/ST.MS.Test.Infra --startup-project Api/src/Microservices/Test/ST.MS.Test.Api
```

## 迁移管理工具

`Api/tools/` 下提供了一键迁移管理脚本，统一管理所有微服务的 EF Core 迁移：

- **PowerShell**：[`Api/tools/migrate.ps1`](../../../Api/tools/migrate.ps1)
- **Bash**：[`Api/tools/migrate.sh`](../../../Api/tools/migrate.sh)

### 前置条件

```bash
dotnet tool install --global dotnet-ef
```

### 用法

```bash
# 检查所有服务是否有未迁移的模型变更
migrate.ps1 check                # PowerShell
./migrate.sh check               # Bash

# 检查指定服务
migrate.ps1 check identity
./migrate.sh check identity

# 列出服务的迁移历史
migrate.ps1 list fileupload
./migrate.sh list fileupload

# 新增迁移
migrate.ps1 add identity AddUserAvatar
./migrate.sh add identity AddUserAvatar

# 移除最后一条迁移（带确认）
migrate.ps1 remove test
./migrate.sh remove test

# 应用所有待处理迁移到数据库
migrate.ps1 update identity
./migrate.sh update identity

# 应用到指定迁移
migrate.ps1 update identity 0003
./migrate.sh update identity 0003

# 生成 SQL 脚本（输出到 scripts/ 目录）
migrate.ps1 script identity
./migrate.sh script identity

# 生成指定范围内的 SQL 脚本
migrate.ps1 script identity 0001 0003
./migrate.sh script identity 0001 0003

# 跳过构建步骤（check 不支持，其余命令均支持）
migrate.ps1 add identity AddUserAvatar --no-build
./migrate.sh add identity AddUserAvatar --no-build
```

### 服务注册

脚本从 [`Api/tools/migrations.json`](../../../Api/tools/migrations.json) 读取服务配置。新增微服务时同步在该文件中注册即可。

| 服务 | DbContext | 数据库 |
|------|-----------|--------|
| `identity` | `IdentityDbContext` | `st_identity` |
| `operationlog` | `OperationLogDbContext` | `st_operationlog` |
| `test` | `AppDbContext` | `st_test` |
| `fileupload` | `FileUploadDbContext` | `st_fileupload` |
