# 部署与运行导航

## 本地开发（与仓库一致）

### 构建后端

在 Monorepo 根目录：

```bash
dotnet build Api/src/ST.slnx
```

在 `Api/` 目录下：

```bash
dotnet build src/ST.slnx
```

### 推荐：Aspire 启动多服务

```bash
dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj
```

### 单服务调试

```bash
dotnet run --project Api/src/Microservices/Identity/ST.MS.Identity.Api/ST.MS.Identity.Api.csproj
```

```bash
dotnet run --project Api/src/Microservices/Test/ST.MS.Test.Api/ST.MS.Test.Api.csproj
```

```bash
dotnet run --project Api/src/Microservices/FileUpload/ST.MS.FileUpload.Api/ST.MS.FileUpload.Api.csproj
```

### 网关

网关项目路径：`Api/src/Microservices/Gateway/ST.Gateway/ST.Gateway.csproj`。生产/预发前置反向代理、TLS 终端、YARP 路由与限流策略；配置节 `ReverseProxy`、`DownstreamServices:*:Address` 等需按环境覆盖（勿将生产密钥提交仓库）。

### 前端

```bash
cd Web
pnpm install
pnpm dev
```

根目录环境变量见 `Web` 下 Vite 约定（`VITE_*`），详见 [`../ai/web/Env.md`](../ai/web/Env.md)。

## 配置与密钥

- 共享配置加载顺序与 UserSecrets/环境变量键名约定见 [`../ai/api/ServiceTemplate.md`](../ai/api/ServiceTemplate.md) 与 [`../ai/api/Auth.md`](../ai/api/Auth.md)。
- 禁止在仓库中保存生产 `Jwt:SigningKey`、数据库连接串、SMTP 密码等；使用环境变量或密钥管理器。

### Aspire 用户机密管理

Aspire AppHost 使用 [`builder.AddParameter()`](../../Api/src/Aspire/ST.Aspire.AppHost/AppHost.cs) 为 Redis、PostgreSQL、RabbitMQ 等容器管理密码。参数值应存储在 .NET 用户机密中，而非 `appsettings.Development.json`，以避免敏感信息被提交到仓库，并防止容器因重启时密码变动而被重建。

**初始化用户机密**（首次运行或克隆后执行一次）：

```bash
cd Api/src/Aspire/ST.Aspire.AppHost
aspire secret set Parameters:password <密码值>
aspire secret set Parameters:pguser <用户名>
aspire secret set Parameters:rabbitUser <RabbitMQ用户>
aspire secret set Parameters:rabbitPassword <RabbitMQ密码>
```

**修改密码**：重新执行 `aspire secret set` 更新对应参数，然后重新创建容器使新密码生效：

```bash
# 修改后需要删除旧容器
docker rm -f ST.Aspire.AppHost_cache ST.Aspire.AppHost_postgres ST.Aspire.AppHost_rabbitmq
# 重新启动 Aspire 即可自动创建使用新密码的容器
dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj
```

**查看当前机密**：用户机密存储在 `%APPDATA%\Microsoft\UserSecrets\<guid>\secrets.json` 中，对应 `.csproj` 中的 `<UserSecretsId>`。

## Docker 镜像构建

CI/CD 自动构建与本地部署流程见 `.github/workflows/build-images-and-deploy.yml`，推送到 `develop` 后自动触发。

### 后端镜像

所有微服务共用 `Api/src/Dockerfile`，通过构建参数选择服务：

```bash
docker build \
  -f Api/src/Dockerfile \
  --build-arg PROJECT=Microservices/Identity/ST.MS.Identity.Api/ST.MS.Identity.Api.csproj \
  --build-arg DLL=ST.MS.Identity.Api.dll \
  -t st-ms-identity-api \
  Api/src/
```

| 服务 | 项目路径 | 镜像名 |
|------|----------|--------|
| Identity API | `Microservices/Identity/ST.MS.Identity.Api` | `st-ms-identity-api` |
| OperationLog API | `Microservices/OperationLog/ST.MS.OperationLog.Api` | `st-ms-operationlog-api` |
| OperationLog Consumer | `Microservices/OperationLog/ST.MS.OperationLog.Consumer` | `st-ms-operationlog-consumer` |
| FileUpload API | `Microservices/FileUpload/ST.MS.FileUpload.Api` | `st-ms-fileupload-api` |
| Test API | `Microservices/Test/ST.MS.Test.Api` | `st-ms-test-api` |
| Gateway | `Microservices/Gateway/ST.Gateway` | `st-gateway` |

### 前端镜像

```bash
docker build -t st-web Web/
```

Nginx 配置见 `Web/nginx.conf`，含 SPA 路由回退与静态资源缓存。

## 可观测性（Observability — 一期日志链路）

实现 **OpenTelemetry + Grafana Alloy + Loki + Grafana** 日志链路。

### 架构

```
.NET 微服务 (OTLP gRPC port 4317)
       │
       ▼
  Grafana Alloy (port 4317)
       │
       ▼
  Loki (port 3100)
       │
       ▼
  Grafana (port 3000, 预置 Loki 数据源)
```

### 配置目录

| 组件 | 配置路径 |
|------|----------|
| Alloy | [`deploy/alloy/config.alloy`](../../deploy/alloy/config.alloy) |
| Loki | [`deploy/loki/loki-config.yaml`](../../deploy/loki/loki-config.yaml) |
| Grafana Datasource | [`deploy/grafana/datasources/loki.yaml`](../../deploy/grafana/datasources/loki.yaml) |

### 启动

```bash
# 完整启动（含可观测性栈）
cd deploy
docker compose up -d

# 仅可观测性栈（基础设施已运行）
docker compose up -d alloy loki grafana
```

### 访问

| 组件 | 地址 | 默认凭据 |
|------|------|----------|
| Grafana | `http://localhost:23000` | `admin / admin123` |
| Loki (HTTP API) | `http://localhost:23100` | 无需认证 |

### 验证

```bash
# 1. 检查容器状态
docker compose -f deploy/docker-compose.yml ps

# 2. 查询 Loki 是否已收到日志
curl -s "http://localhost:23100/loki/api/v1/query_range?query=%7Bsource%3D%22st-alloy%22%7D&limit=5" | jq

# 3. Grafana Explore
#   打开 http://localhost:23000 → Explore → 选择 Loki 数据源
#   查询: {source="st-alloy"}
```

### LogQL 查询示例

```logql
# 按服务名查询
{service_name="st-ms-identity-api"}

# 按日志级别过滤
{service_name="st-ms-identity-api"} |= "Error"

# 按 TraceId 检索
{service_name=~".+"} |= "TraceId=abc123"

# 查询过去 15 分钟错误日志
{source="st-alloy"} |= "fail" |= "Error"
```

### 环境变量

新增以下 `.env` 变量（`deploy/.env` / `deploy/.env.example`）：

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `GRAFANA_ADMIN_USER` | `admin` | Grafana 管理员用户名 |
| `GRAFANA_ADMIN_PASSWORD` | `admin123` | Grafana 管理员密码 |
| `GRAFANA_HOST_PORT` | `23000` | Grafana 宿主机端口 |
| `LOKI_HOST_PORT` | `23100` | Loki 宿主机端口 |
| `ALLOY_OTLP_GRPC_PORT` | `24317` | Alloy OTLP gRPC 宿主机端口 |
| `ALLOY_OTLP_HTTP_PORT` | `24318` | Alloy OTLP HTTP 宿主机端口 |

### 启用 OTLP 导出

.NET 微服务通过 `OTEL_EXPORTER_OTLP_ENDPOINT=http://alloy:4317` 环境变量启用 OTLP 日志导出。已在 `docker-compose.yml` 中为每个微服务设置此变量。

开启条件（代码见 `WebApplicationBuilderExtensions.cs`）：
- 设置 `OTEL_EXPORTER_OTLP_ENDPOINT` 后自动启用 `builder.Logging.AddOpenTelemetry()`
- 同时启用 Metrics（AspNetCore + HttpClient + Runtime）和 Tracing（AspNetCore + HttpClient）

### 二阶段规划

| 阶段 | 内容 | 接入点 |
|------|------|--------|
| 一期（本期） | 日志链路 OTLP → Alloy → Loki → Grafana | Alloy:4317 gRPC |
| 二期 | Prometheus metrics + 告警规则 | Alloy 已预留 batch processor |
| 三期 | Tempo trace 链路 + 日志-追踪联动 | Loki derived fields → Tempo |

## AI 注意

- 部署清单、K8s、Helm 等可在此目录下按环境追加子文档；**与 `docs/ai/common/Monorepo.md` 的目录约定不冲突**即可。


## CI/CD 工作流

`.github/workflows/build-images-and-deploy.yml` 包含 5 个 Job：

| Job | 触发条件 | 说明 |
|-----|----------|------|
| `test-backend` | push / workflow_dispatch | 单元测试门禁（无测试项目时跳过） |
| `test-frontend` | push / workflow_dispatch | TypeScript 类型检查 + ESLint / OxLint |
| `build-backend` | push / workflow_dispatch | 矩阵构建 6 个后端镜像 → GHCR |
| `build-frontend` | push / workflow_dispatch | 构建 st-web 前端镜像 → GHCR |
| `deploy-local` | push to develop | 拉取镜像 → 重启容器 → 迁移 → 数据种子 → 健康检查 |
| `cleanup` | 始终执行（含 schedule） | 清理 GHCR 上旧的 `dev-*` 版本，保留最近 10 个 |

> **Schedule 触发（每日 06:00 UTC）**：仅执行 `cleanup` Job，跳过构建与部署。

### 数据种子控制

各服务通过环境变量 `App__IsDataSeed` 控制是否在启动时执行种子数据：

- `docker-compose.yml` 中引用 `${APP_IS_DATA_SEED:-false}`，由 `.env` 文件控制
- 生产 CI/CD 流程：初始 `.env` 写入 `APP_IS_DATA_SEED=false` → 容器启动时不 seed → 迁移完成后设为 `true` → 重启业务容器触发 seed
- 种子数据使用 `WHERE NOT EXISTS` / `AnyAsync` 进行幂等检查，重复执行安全
- 本地开发可在 `deploy/.env` 中手动设为 `true` 以在 `docker compose up` 时直接 seed

当前已注册种子：
- **Identity**：权限数据、admin 角色、默认用户（SQL 脚本）
- **Test**：`TestSampleDataSeed`（示例实体）

### EF Core 迁移

迁移在 `deploy-local` Job 中按以下步骤执行：

1. 启动所有容器（含 PostgreSQL）
2. 等待 PostgreSQL 就绪（TCP 端口探测）
3. 检测本地 .NET SDK 缓存 → 未命中时通过 `setup-dotnet` 安装
4. 安装 `dotnet-ef` 工具到 `--tool-path`
5. 分别 restore 4 个 Infra 项目 → 对 Identity / OperationLog / FileUpload / Test 执行 `dotnet ef database update`
6. 迁移完成后启用 seed → 重启业务容器

`deploy/docker-compose.yml` 保持 `App__IsCodeFirst=false` 与 `App__IsCreateDatabase=false`，避免容器启动时自动迁移。

### 健康检查

迁移与 seed 完成后，使用 `Invoke-WebRequest` 轮询网关（`GATEWAY_HOST_PORT`），确认服务已正常启动。

### 清理

`cleanup` Job 通过 `gh api` 查询 GHCR 上每个服务的 `dev-*` 标签版本，超出 10 个旧版本时自动删除，避免镜像仓库膨胀。支持 `workflow_dispatch` 的 `dry_run` 输入进行演练。

### GitHub Environment 变量

推荐在 `ST Secrets` Environment 中配置（未配置时使用默认值）：

| 变量 | 默认值 | 说明 |
|------|--------|------|
| `POSTGRES_HOST_PORT` | `25432` | PostgreSQL 宿主机端口 |
| `IDENTITY_DB_NAME` | `st_identity` | Identity 数据库名 |
| `OPERATIONLOG_DB_NAME` | `st_operationlog` | OperationLog 数据库名 |
| `FILEUPLOAD_DB_NAME` | `st_fileupload` | FileUpload 数据库名 |
| `TEST_DB_NAME` | `st_test` | Test 数据库名 |

这样可将迁移行为从应用启动彻底解耦，并通过明确顺序（迁移 → seed → 健康检查）保证发布可靠性。
