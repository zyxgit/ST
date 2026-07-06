# 部署与运行文档

## 本地构建

```bash
dotnet build Api/src/ST.slnx
```

```bash
cd Web
pnpm install
pnpm build
```

## Aspire 启动

```bash
dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj
```

Aspire AppHost 负责本地编排 PostgreSQL、Redis、RabbitMQ 和微服务。敏感参数应通过 .NET User Secrets 或环境变量提供，禁止提交真实密码。

## Docker Compose

```bash
cd deploy
docker compose up -d
```

当前 Compose 包含：PostgreSQL、Redis、RabbitMQ、Alloy、Loki、Prometheus、Grafana、Identity、OperationLog、OperationLog.Consumer、FileUpload、Test、Order、Inventory、Payment、Gateway、Web。

## 镜像

后端服务使用 `Api/src/Dockerfile`，通过构建参数选择项目和 DLL。CI/CD 中镜像推送到 GHCR，服务名形如：

- `st-ms-identity-api`
- `st-ms-operationlog-api`
- `st-ms-operationlog-consumer`
- `st-ms-fileupload-api`
- `st-ms-test-api`
- `st-ms-order-api`
- `st-ms-inventory-api`
- `st-ms-payment-api`
- `st-gateway`
- `st-web`

## 环境变量与密钥

- 生产数据库连接串、JWT SigningKey、SMTP 密码、对象存储密钥只能通过环境变量、User Secrets 或密钥管理器注入。
- 文档和示例只能写占位符或键名。
- 新增环境变量必须说明：名称、作用、默认值、是否敏感、影响服务。

## 可观测性

本地可观测性栈：

```text
.NET 服务 → OTLP → Grafana Alloy → Loki / Prometheus → Grafana
```

常用命令：

```bash
cd deploy
docker compose up -d alloy loki prometheus grafana
```

Grafana 默认入口通常由 Compose 环境变量控制，当前部署文档以 `deploy/docker-compose.yml` 和 `.env` 为准。

## 网关部署注意

- 生产环境应由外层反向代理或负载均衡终止 TLS。
- `ForwardedHeaders` 必须按真实代理 IP 配置，不要随意开启 TrustAll。
- CORS AllowedOrigins 必须使用明确域名，不使用生产 `*`。
- 限流模式可使用 Redis，规则在 Gateway 配置中维护。

## 发布前检查

- [ ] 后端构建通过。
- [ ] 前端构建通过。
- [ ] Docker Compose 配置无真实密钥。
- [ ] 新服务已加入 Gateway、Aspire、Compose。
- [ ] 数据库迁移已生成并有执行说明。
- [ ] 可观测性字段和日志脱敏已检查。
