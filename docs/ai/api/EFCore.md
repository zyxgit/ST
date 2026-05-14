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

### 使用迁移工具一键检测和生成

为了简化迁移管理，项目提供了 **MigrationHelper.ps1** 脚本，支持：
- **检测迁移状态**：识别所有或指定微服务中有待生成的迁移
- **批量生成迁移**：一命令为多个服务生成迁移
- **自动编号**：无需手动指定迁移名称时自动递增

**快速使用**：

```bash
cd Api

# 检测所有服务的迁移状态
.\tools\MigrationHelper.ps1

# 为所有待迁移的服务生成迁移
.\tools\MigrationHelper.ps1 -Generate

# 为指定服务生成迁移，指定名称
.\tools\MigrationHelper.ps1 -Generate -Service Identity -Message "AddUserAvatar"

# 为多个服务生成迁移
.\tools\MigrationHelper.ps1 -Generate -Service Identity,FileUpload
```

详见 [`Api/tools/README.md`](../../Api/tools/README.md)。

### 连接字符串解析（设计时）

设计时工厂继承自 `NpgsqlDesignTimeDbContextFactoryBase<TContext>`，默认通过 `IConfiguration` 按以下优先级获取连接字符串（高覆盖低）：

```
Environment Variables  ← 最高优先级（Aspire/CI/脚本注入）
User Secrets           ← Infra 项目的 UserSecretsId
appsettings.Development.json  ← 启动项目（*.Api）
appsettings.json       ← 启动项目（*.Api）
```

底层由 `DatabaseConnectionInfoResolver`(与运行时一致) 处理回退逻辑。

### 本地开发设置

开发人员需要为每个微服务设置连接字符串，**推荐使用 User Secrets**：

```bash
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=15432;Username=postgres;Password=<密码>;Database=st_identity" --project "Api/src/Microservices/Identity/ST.MS.Identity.Infra"
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=15432;Username=postgres;Password=<密码>;Database=st_operationlog" --project "Api/src/Microservices/OperationLog/ST.MS.OperationLog.Infra"
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=15432;Username=postgres;Password=<密码>;Database=st_fileupload" --project "Api/src/Microservices/FileUpload/ST.MS.FileUpload.Infra"
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=15432;Username=postgres;Password=<密码>;Database=st_test" --project "Api/src/Microservices/Test/ST.MS.Test.Infra"
```

亦可通过环境变量 `Database__ConnectionString` 设置（适用于 CI/Docker）。

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
	
