# architecture.skill

## 1. Skill Name

`st-architecture-monorepo` — ST 仓库结构、运行时拓扑与演进边界。

## 2. Purpose

- 固定 Monorepo 边界、Aspire/网关/微服务职责，防止 Agent 生成错误路径或重复解决方案文件。
- 作为检索入口链到其他 Skill。

## 3. Tech Stack

| 边界 | 路径 / 技术 |
|------|----------------|
| Monorepo 根 | `ST/`，单 Git；子目录 `Api/`、`Web/`、`docs/` |
| 后端解决方案 | `Api/src/ST.slnx`（非根目录 `.sln`） |
| 编排 | Aspire：`Api/src/Aspire/ST.Aspire.AppHost` |
| 网关 | `Api/src/Microservices/Gateway/ST.Gateway`，YARP、`ReverseProxy` 配置节 |
| 前端应用 | `Web/`，pnpm + Vite |

## 4. Architecture Rules

- **不分 Submodule**；`Api`、`Web` 为普通目录。
- **流量**：对外入口通常为网关 → 下游集群地址（`DownstreamServices:*` + 内存覆盖映射）；开发可用 Aspire 或直接跑单服务。
- **微服务**：每服务四层项目；共享能力在 `ServiceShared` 与 `Infrastructures`。
- **横向**：OpenTelemetry、健康检查来自 `AddServiceDefaults`（Aspire ServiceDefaults）。

## 5. Coding Rules

- 新增下游服务：网关 `ReverseProxy` 路由 + `ApplyGatewayDestinationOverrides` 映射键需与环境变量一致。
- 本地 CLI：`dotnet build Api/src/ST.slnx`；Aspire：`dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj`。

## 6. Naming Rules

- 微服务文件夹：`Microservices/<BoundedContext>/`。
- 集群/路由 id：`identity-cluster`、`operationlog-cluster` 等（以现有 `appsettings` 为准，勿随意改名除非同步配置）。

## 7. Best Practices

- 新功能优先落在**单一微服务**；跨服务用 HTTP/消息，不共享 DbContext。
- 文档与代码同 PR（`DocumentationSync.md`）。

## 8. Forbidden Practices

- 在仓库根再创建聚合 `ST.sln` 除非团队重新引入（当前以 `ST.slnx` 为准）。
- 前端硬编码多个生产微服务 base URL 绕过网关（除本地调试且文档说明）。
- 将网关密钥、下游证书私钥写入仓库。

## 9. AI Generation Constraints

- 任何路径引用须以 `Api/src/...`、`Web/src/...` 开头验证存在性。
- 生成部署说明时必须提及网关与 `ForwardedHeaders`（若前置代理）。
- 不臆造不存在的微服务名；新增服务须同步 `docs/architecture/README.md`。

## 10. Example Code

```csharp
// Gateway Program.cs — YARP 注册（节选）
builder.Services.AddReverseProxy()
	.LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
// ...
app.MapReverseProxy();
```

```bash
# 构建
dotnet build Api/src/ST.slnx
```

## 11. Related Documents

- `docs/architecture/README.md`
- `docs/ai/common/Monorepo.md`、`Architecture.md`
- `docs/deploy/README.md`
- `docs/ai/common/Architecture.md`
