# 微服务模板（新建服务指南）

## 目录

- [目录结构](#目录结构)
- [Aspire 编排注册](#aspire-编排注册)
- [Program.cs 模板](#programcs-模板)
- [InfraModule 模板](#inframodule-模板)
- [共享配置](#共享配置)
- [密钥与环境变量](#密钥与环境变量)
- [appsettings 模板](#appsettings-模板)
- [授权用法](#授权用法)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 目录结构

本仓库所有微服务遵循统一四层结构：

| 层 | 项目 | 职责 |
|----|------|------|
| API | `*.Api` | HTTP 入口、认证、OpenAPI |
| Application | `*.Application` | 应用服务、DTO、用例编排、模块 |
| Domain | `*.Domain` | 实体、领域规则、领域事件 |
| Infra | `*.Infra` | DbContext、仓储实现、外部适配器、模块 |

现有示例：`Identity`（`ST.MS.Identity.*`）和 `Test`（`ST.MS.Test.*`），位于 `Api/src/Microservices/`。

## Aspire 编排注册

新微服务必须在 Aspire AppHost 中注册，才能在本地开发时被编排启动。

**注意**：注册 Aspire 后还需注册网关路由，见下方 [网关注册](#网关注册)。

**AppHost.cs 注册**（`Api/src/Aspire/ST.Aspire.AppHost/AppHost.cs`）：

```csharp
builder.AddProject<Projects.ST_MS_YourService_Api>("st-ms-yourservice-api")
    .WaitFor(redis)
    .WaitFor(postgres)
    .WaitFor(rabbitMq);
```

**csproj 项目引用**（`ST.Aspire.AppHost.csproj`）：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\Microservices\YourService\ST.MS.YourService.Api\ST.MS.YourService.Api.csproj" />
</ItemGroup>
```

- `Projects.ST_MS_YourService_Api` 是 Aspire 自动生成的类型，基于项目引用，命名规则为 `.` → `_`。
- 如果服务不需要 Redis/RabbitMQ，可移除对应的 `.WaitFor(...)`。

## 网关注册

所有微服务需在 Gateway（YARP）中注册路由，才能通过统一网关入口对外暴露。

### 必需修改的文件

| 文件 | 修改内容 |
|------|----------|
| `Microservices/Gateway/ST.Gateway/appsettings.json` | 添加 `DownstreamServices`、`ReverseProxy:Routes`、`ReverseProxy:Clusters` |
| `Microservices/Gateway/ST.Gateway/Program.cs` | `ApplyGatewayDestinationOverrides` 添加映射、`ResolveRequestScope` 添加路径匹配、添加 docs 重定向 |
| `Microservices/Gateway/ST.Gateway/wwwroot/docs/index.html` | 新增服务卡片（含 Scalar + OpenAPI 链接） |

### 操作步骤

**1. `appsettings.json` — 下游地址**

```json
{
  "DownstreamServices": {
    "YourService": {
      "Address": "https://localhost:7xxx"
    }
  }
}
```

**2. `appsettings.json` — Routes 与 Clusters**

Routes（API 路由 + Docs 路由）：

```json
{
  "ReverseProxy": {
    "Routes": {
      "yourservice-api-route": {
        "ClusterId": "yourservice-cluster",
        "Match": { "Path": "/api/yourservice/{**catch-all}" },
        "Transforms": [ { "PathRemovePrefix": "/api/yourservice" } ]
      },
      "yourservice-docs-route": {
        "ClusterId": "yourservice-cluster",
        "Match": { "Path": "/docs/yourservice/{**catch-all}" },
        "Transforms": [ { "PathRemovePrefix": "/docs/yourservice" } ]
      }
    },
    "Clusters": {
      "yourservice-cluster": {
        "Destinations": {
          "yourservice-destination": {
            "Address": "https://localhost:7xxx"
          }
        }
      }
    }
  }
}
```

> **注意**：`PathRemovePrefix` 移除的前缀需与控制器 `[Route]` 一致。例如控制器 `[Route("api/files")]` 则路由前缀为 `/api/files`。
>
> **前端 URL 约定**：前端 API 路径使用 `/{service-name}/api/{controller-path}` 格式（在 service name 后加 `api/`），经 Axios `baseURL=/api` 后可被网关正确路由。详见 [`web/Env.md`](../web/Env.md#本地代理)。

**3. `Program.cs` — 目标地址覆盖**

```csharp
// ApplyGatewayDestinationOverrides
["ReverseProxy:Clusters:yourservice-cluster:Destinations:yourservice-destination:Address"] = configuration["DownstreamServices:YourService:Address"]
```

**4. `Program.cs` — Docs 重定向**

```csharp
var yourserviceDocsRedirect = app.MapGet("/docs/yourservice", () => Results.Redirect("/docs/yourservice/scalar/v1")).ExcludeFromDescription();
// 限流：
yourserviceDocsRedirect.RequireRateLimiting("gateway-local-docs");
```

**5. `Program.cs` — 路径范围解析**

```csharp
if (pathValue.StartsWith("/api/yourservice", StringComparison.OrdinalIgnoreCase) ||
    pathValue.StartsWith("/docs/yourservice", StringComparison.OrdinalIgnoreCase))
{
    return "yourservice";
}
```

**6. `wwwroot/docs/index.html` — 文档入口卡片**

在 `<div class="grid">` 中新增服务卡片，参考现有卡片模式：

```html
<section class="card">
  <h2>YourService</h2>
  <a href="/docs/yourservice/scalar/v1" target="_blank" rel="noopener noreferrer">Scalar</a>
  <a href="/docs/yourservice/openapi/v1.json" target="_blank" rel="noopener noreferrer">OpenAPI</a>
</section>
```

### 验证

网关启动后访问 `http://localhost:5099/docs/yourservice/scalar/v1` 应看到服务的 Swagger 页面。

---

## Program.cs 模板

```csharp
using NLog;
using Scalar.AspNetCore;
using ST.Shared.Module;
using ST.Shared.WebApi.Extensions;

// 你的服务模块：
using Your.Service.Application;
using Your.Service.Domain;
using Your.Service.Infra;

try
{
    var builder = WebApplication.CreateBuilder(args);

    var modules = new ISharedModule[]
    {
        new ApplicationModule(),
        new DomainModule(),
        new InfraModule()
    };

    builder.AddServiceDefaults();
    builder.AddSharedWebApi(modules);

    // 可选服务级扩展：
    // builder.Services.AddOpenApi();

    var app = builder.Build();
    app.MapDefaultEndpoints();
    app.UseSharedWebApi(modules);

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    LogManager.GetCurrentClassLogger().Error(ex);
}
finally
{
    LogManager.Shutdown();
}
```

关键入口：
- `AddServiceDefaults()` — Aspire 遥测、健康检查、服务发现（见 `ST.Aspire.ServiceDefaults`）
- `AddSharedWebApi(modules)` — 注册 NLog、Autofac 容器、Redis、后台任务
- `UseSharedWebApi(modules)` — 注册全局异常中间件、请求日志、HTTPS 重定向、授权、控制器映射

## InfraModule 模板

```csharp
using Microsoft.Extensions.DependencyInjection;
using ST.Infra.EntityFramework.Npgsql.Extensions;
using ST.Shared.Module;

namespace Your.Service.Infra;

public sealed class InfraModule : ServiceModule
{
    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        // 要求：
        // - Database:Provider（默认来自共享配置）
        // - Database:ConnectionString（建议 UserSecrets / 环境变量）
        services.AddNpgsqlDbContextFromConfig<YourDbContext>();
    }
}
```

`ApplicationModule` 与 `DomainModule` 同理继承 `ServiceModule`，在 `ConfigureServices` 中注册各自服务。

## 共享配置

共享默认值位于：

```
Api/src/ServiceShared/ST.Shared/Config/appsettings.Shared.json
```

所有使用 `builder.AddSharedWebApi(...)` 的服务自动加载为低优先级配置源。

覆盖优先级（从低到高）：

1. `ST.Shared` 内嵌共享配置
2. 服务 `appsettings.json`
3. 服务 `appsettings.Development.json`
4. 环境变量

## 密钥与环境变量

推荐密钥入口（禁止硬编码到仓库）：

| 配置键 | 用途 |
|--------|------|
| `Database__ConnectionString` | 数据库连接串 |
| `Jwt__SigningKey` | JWT 签名密钥 |
| `Smtp__Password` | SMTP 密码 |
| `RabbitMQ__EventBus__Password` | EventBus 密码 |
| `RabbitMQ__OperationLog__Password` | 操作日志 RabbitMQ 密码 |

参考示例值见 `config/env.shared.example`。

## appsettings 模板

**`appsettings.json`**（提交到仓库，仅放非敏感默认值，其余由共享配置 `appsettings.Shared.json` 提供）：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**`appsettings.Development.json`**（本地开发配置，含数据库连接串等敏感默认值，**不提交到生产仓库**）：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Database": {
    "ConnectionString": "Host=localhost;Port=15432;Username=postgres;Password=pw123456;Database=st_yourservicename;"
  }
}
```

- `Database:Provider` 来自共享配置 `appsettings.Shared.json`（默认 `Npgsql`），无需在服务中重复设置。
- `appsettings.json` 只放置服务特有的非敏感配置（如 `FileStorage`），通用配置（`App`、`Jwt`）由共享配置源自动加载。
- 数据库连接串优先通过环境变量 `Database__ConnectionString` 覆盖。

## DesignTimeDbContextFactory 模板

用于 EF Core 迁移工具在开发环境获取连接串（每个微服务 Infra 层各一份）：

```csharp
using Microsoft.EntityFrameworkCore;
using ST.Infra.EntityFramework.Npgsql.DesignTime;
using ST.Shared.Const;

namespace Your.Service.Infra;

public sealed class YourServiceDesignTimeDbContextFactory : NpgsqlDesignTimeDbContextFactoryBase<YourDbContext>
{
    protected override string GetConnectionString(string[] args)
    {
        return Environment.GetEnvironmentVariable(SettingPrefixContants.Database_ConnectionString_Env)
               ?? "Host=localhost;Port=15432;Username=postgres;Password=pw123456;Database=st_yourservicename;";
    }

    protected override YourDbContext CreateDbContext(DbContextOptions options, string[] args)
    {
        return new YourDbContext(options);
    }
}
```

## 授权用法

- 要求登录：`[Authorize]`
- 角色检查：`[Authorize(Roles = "admin")]`
- 权限检查：`[Authorize(Policy = "perm:user:create")]`

权限 Policy 使用 `perm:` 前缀约定，详见 [Auth.md](./Auth.md)。

## 推荐方案

- 复制现有微服务（`Identity` 或 `Test`）的完整项目结构，全局替换命名空间。
- 新服务的 `*.Api` 引用 `ST.Aspire.ServiceDefaults` 和 `ST.Shared.WebApi`。
- 新服务的 `*.Application` 只引用 `ST.Shared.Application` 和自身 `*.Domain`。
- 新服务的 `*.Infra` 引用 `ST.Infra.EntityFramework.Npgsql`（若需 EF）和自身 `*.Domain`。
- `appsettings.json` 仅放服务特有非敏感配置；`appsettings.Development.json` 加上 `Database:ConnectionString`。
- Infra 层创建 `DesignTimeDbContextFactory`，为 EF 迁移工具提供连接串。
- Aspire AppHost 添加项目引用与 `builder.AddProject<>()` 注册。

## 禁止事项

- 禁止跳过 `ISharedModule` 注册体系直接在 `Program.cs` 中注册大量服务。
- 禁止将生产连接串、SigningKey、密码提交到 Git。
- 禁止跨微服务共享 DbContext 或数据库表。

## AI 注意事项

- 生成新服务时，严格遵循本模板的 **Program.cs / InfraModule** 形态。
- **`appsettings.json`** 保持最小化，仅含服务特有配置；**`appsettings.Development.json`** 加入 `Database:ConnectionString`，数据库名按 `st_服务名小写` 命名。
- **Infra 层**必须创建 `DesignTimeDbContextFactory`，用于 EF 迁移连接串回退。
- **Aspire AppHost** 需同时更新 csproj 引用和 `AppHost.cs` 注册。
- 不确定依赖方向时，查阅 [Architecture.md](../common/Architecture.md) 与现有服务。
- 新建服务后，同步更新 `docs/architecture/README.md` 和 `docs/deploy/README.md`。
