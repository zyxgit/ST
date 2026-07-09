# 后端服务模板与防 502 清单

本文用于指导 AI 或开发者创建新微服务。目标是避免“代码能编译，但 Gateway 访问 502”的常见问题。

## 先判断是否真的需要新服务

创建新服务前必须回答：

1. 是否已有 bounded context 可以承载该能力？
2. 是否需要独立数据库、独立部署、独立扩缩容？
3. 是否会产生跨服务事务、消息、缓存、权限或前端菜单变更？
4. 是否能在同一任务中完成 Gateway、Aspire、Docker Compose、配置、迁移、文档和验证？

如果无法一次交付运行链路，应先拆成“服务骨架”“核心 API”“网关/部署接入”“验证与文档”几个小任务。

## 标准服务目录

```text
Api/src/Microservices/<Service>/
├── ST.MS.<Service>.Api
│   ├── Controllers/
│   ├── Program.cs
│   ├── appsettings.json
│   └── NLog/
├── ST.MS.<Service>.Application
│   ├── Dto/
│   └── Services/
├── ST.MS.<Service>.Domain
│   ├── Entities/
│   └── Enums/
└── ST.MS.<Service>.Infra
    ├── DbContext/
    └── Migrations/
```

## Program.cs 必须包含的启动链

新 API 服务应优先复用现有服务模式：

```csharp
var builder = WebApplication.CreateBuilder(args);

var modules = new ISharedModule[]
{
    new ApplicationModule(),
    new DomainModule(),
    new InfraModule()
};

builder.AddServiceDefaults();
builder.AddSharedWebApi(modules);
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerAuthDocumentTransformer>();
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseSharedWebApi(modules);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.Run();
```

按需增加：

- RabbitMQ：`AddRabbitMqEventBus`。
- 操作日志投递：`AddRabbitMqOperationLogSink`。
- Outbox Publisher：`AddOutboxPublisher`。
- Redis：`AddRedisInfra` 或领域专用扩展。
- 后台任务：`AddHostedService<T>` 并提供配置项。

## 解决方案与项目引用

新服务必须：

- 加入 `Api/src/ST.slnx`。
- Api 引用 Application、Domain、Infra、Shared WebApi、必要基础设施项目。
- Application 只引用 Domain、共享 DTO/抽象、IntegrationEvents 等必要项目。
- Infra 引用 Domain、EF、Repository、ReliableMessaging 等实现依赖。
- Domain 不依赖 Api、Infra、Redis、RabbitMQ、HTTP。

## Gateway 接入，防 502 关键点

502 通常不是 Controller 代码问题，而是 Gateway 无法连到下游服务或协议/端口配置错误。新增服务必须同时更新：

1. `DownstreamServices:<Service>:Address`。
2. `ReverseProxy:Clusters:<service>-cluster:Destinations`。
3. `ReverseProxy:Routes:<service>-api-route`。
4. Docker Compose 服务名、端口、网络、依赖。
5. Aspire AppHost 项目注册与引用基础设施。
6. 服务自身 `launchSettings` / `ASPNETCORE_URLS` / Compose 端口与 Gateway 地址保持一致。

### Gateway 配置模板

```json
{
  "DownstreamServices": {
    "Catalog": {
      "Address": "http://localhost:5093"
    }
  },
  "ReverseProxy": {
    "Routes": {
      "catalog-api-route": {
        "ClusterId": "catalog-cluster",
        "Match": {
          "Path": "/api/catalog/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/api/catalog" },
          { "PathPrefix": "/api" }
        ]
      },
      "catalog-docs-route": {
        "ClusterId": "catalog-cluster",
        "Match": {
          "Path": "/docs/catalog/{**catch-all}"
        },
        "Transforms": [
          { "PathRemovePrefix": "/docs/catalog" }
        ]
      }
    },
    "Clusters": {
      "catalog-cluster": {
        "Destinations": {
          "catalog-destination": {
            "Address": "http://localhost:5093"
          }
        }
      }
    }
  }
}
```

## 502 排查顺序

1. 直接访问下游服务健康检查：`curl -i http://localhost:<port>/health`。
2. 直接访问下游服务 OpenAPI/Scalar，确认服务已启动。
3. 检查 Gateway `DownstreamServices` 和 `Clusters` 端口是否与服务实际监听端口一致。
4. 检查 Gateway 与服务协议是否一致：`http` 不要误写成 `https`，反之亦然。
5. Docker Compose 场景下确认 Gateway 使用容器服务名和容器端口，而不是宿主机 localhost。
6. 检查服务是否缺少数据库、Redis、RabbitMQ 等依赖导致启动失败。
7. 检查 Gateway 日志中的 YARP destination 错误。

## 新服务验收清单

- [ ] 下游服务直接访问 `/health` 返回成功。
- [ ] 下游服务直接访问至少一个业务 API 成功。
- [ ] Gateway 访问同一业务 API 成功。
- [ ] `/docs/<service>` 可进入服务文档。
- [ ] Aspire 可启动该服务。
- [ ] Docker Compose 可启动该服务。
- [ ] `dotnet build Api/src/ST.slnx` 通过。
- [ ] 文档同步更新。
