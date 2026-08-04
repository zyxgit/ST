# DevOps 文档

本文档覆盖本地运行、Docker Compose 部署、环境变量配置和可观测性栈。

## 本地运行方式

### 方式一：Aspire（推荐开发使用）

Aspire 提供本地编排，自动启动 PostgreSQL、Redis、RabbitMQ 和所有微服务：

```bash
dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj
```

Aspire Dashboard 默认地址：`http://localhost:15000`

### 方式二：Docker Compose

适用于模拟生产环境或不依赖 .NET SDK 的场景：

```bash
cd deploy
cp .env.example .env   # 按需修改端口和密码
docker compose up -d
```

### 方式三：混合模式

基础设施用 Docker Compose，微服务用 Aspire 或直接 `dotnet run`：

```bash
# 仅启动基础设施
cd deploy
docker compose up -d postgres redis rabbitmq

# 另一终端启动微服务
dotnet run --project Api/src/Microservices/Identity/ST.MS.Identity.Api/ST.MS.Identity.Api.csproj
```

## Docker Compose 服务清单

| 服务 | 镜像 | 默认端口 | 说明 |
|------|------|----------|------|
| postgres | postgres:18.3 | 25432 | PostgreSQL 数据库 |
| redis | redis:8.6 | 26379 | 缓存、限流、Lua 预扣 |
| rabbitmq | rabbitmq:4.3-management-alpine | 25672 (AMQP) / 25673 (管理界面) | 消息队列 |
| alloy | grafana/alloy:v1.8.1 | 24317 (gRPC) / 24318 (HTTP) | OpenTelemetry Collector |
| loki | grafana/loki:3.4.2 | 23100 | 日志存储 |
| prometheus | prom/prometheus:v3.3.0 | 29090 | 指标存储 |
| grafana | grafana/grafana:11.5.2 | 23000 | 可视化面板 |
| st-gateway | 自构建 | 25000 | YARP 网关入口 |
| st-ms-identity-api | 自构建 | 27127 | 用户、认证、租户 |
| st-ms-operationlog-api | 自构建 | 21001 | 操作日志 API |
| st-ms-operationlog-consumer | 自构建 | — | 操作日志消费者 |
| st-ms-fileupload-api | 自构建 | 27250 | 文件上传 |
| st-ms-test-api | 自构建 | 25089 | 示例服务 |
| st-ms-order-api | 自构建 | 25090 | 订单服务 |
| st-ms-inventory-api | 自构建 | 25091 | 库存服务 |
| st-ms-payment-api | 自构建 | 25092 | 支付服务 |
| st-web | 自构建 | 28080 | Vue 前端 |

## 环境变量

环境变量在 `deploy/.env` 中配置（从 `.env.example` 复制）。关键变量：

### 数据库与中间件

| 变量 | 说明 | 示例值 |
|------|------|--------|
| `PGUSER` | PostgreSQL 用户名 | `st_user` |
| `PGPASSWORD` | PostgreSQL 密码 | `change_me_password` |
| `REDIS_PASSWORD` | Redis 密码 | `change_me_redis` |
| `RABBITUSER` | RabbitMQ 用户名 | `st_rabbit` |
| `RABBITPASSWORD` | RabbitMQ 密码 | `change_me_rabbit` |

### JWT

| 变量 | 说明 |
|------|------|
| `JWTSIGNINGKEY` | JWT 签名密钥（生产环境必须更换） |

### 应用配置

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `APP_IS_DATA_SEED` | 是否启用数据种子 | `false` |
| `VITE_API_BASE_URL` | 前端访问网关地址（留空使用 nginx 代理） | 空 |

### 可观测性

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `GRAFANA_ADMIN_USER` | Grafana 管理员用户名 | `admin` |
| `GRAFANA_ADMIN_PASSWORD` | Grafana 管理员密码 | `change_me_grafana` |

### 端口映射

所有服务端口可通过环境变量自定义，避免与宿主机已有服务冲突：

```text
POSTGRES_HOST_PORT=25432
REDIS_HOST_PORT=26379
RABBITMQ_AMQP_HOST_PORT=25672
RABBITMQ_MGMT_HOST_PORT=25673
IDENTITY_HOST_PORT=27127
OPERATIONLOG_HOST_PORT=21001
FILEUPLOAD_HOST_PORT=27250
TEST_HOST_PORT=25089
GATEWAY_HOST_PORT=25000
WEB_HOST_PORT=28080
GRAFANA_HOST_PORT=23000
LOKI_HOST_PORT=23100
ALLOY_OTLP_GRPC_PORT=24317
ALLOY_OTLP_HTTP_PORT=24318
PROMETHEUS_HOST_PORT=29090
```

## 可观测性栈

### 架构

```text
微服务 → OpenTelemetry SDK → Alloy (OTLP) → Loki (日志) / Prometheus (指标) → Grafana (面板)
```

### 组件说明

| 组件 | 职责 |
|------|------|
| OpenTelemetry SDK | 内置于微服务，采集日志、指标、Trace |
| Alloy | Grafana 的 OpenTelemetry Collector，接收 OTLP 数据并转发 |
| Loki | 日志存储，通过 Alloy 接收结构化日志 |
| Prometheus | 指标存储，从 Alloy 抓取或接收 OTLP 指标 |
| Grafana | 可视化，预配置 Loki/Prometheus 数据源和仪表盘 |

### 预置仪表盘

Grafana 自动加载以下仪表盘（位于 `deploy/grafana/provisioning/dashboards/`）：

| 仪表盘 | 文件 | 用途 |
|--------|------|------|
| ST Overview | `st-overview.json` | 全局服务健康概览 |
| ST Gateway | `st-gateway.json` | 网关请求、延迟、错误率 |
| ST OperationLog | `st-operationlog.json` | 操作日志消费、死信 |
| ST Order Saga | `st-order-saga.json` | 订单 Saga 状态流转 |

### 访问地址

启动 Docker Compose 后：

| 服务 | 地址 |
|------|------|
| Grafana | `http://localhost:23000` |
| Prometheus | `http://localhost:29090` |
| Loki | `http://localhost:23100` |
| RabbitMQ 管理界面 | `http://localhost:25673` |

### 日志查询示例（Grafana Explore → Loki）

```logql
{job="st-ms-identity-api"} |= "ERROR"
{service_name="st-gateway"} | json | duration > 1s
```

## 数据库迁移

每个微服务独立管理自己的数据库和迁移。首次启动时设置环境变量自动建库：

```env
App__IsCodeFirst=true
App__IsCreateDatabase=true
App__IsDataSeed=true
```

生产环境建议使用迁移脚本：

```bash
# 批量迁移所有服务
./deploy/migrate-all.sh
```

## 2C2G 低配部署

针对 2 核 2G 内存的服务器，提供精简配置：

```bash
cd deploy
cp .env.2c2g.example .env
docker compose -f docker-compose.2c2g.yml up -d
```

详见 `deploy/DEPLOY-CHECKLIST-2C2G.md`。

## 常见问题

### 502 Bad Gateway

- 检查下游服务是否启动：`docker compose ps`
- 检查 Gateway 配置的下游地址是否正确
- 检查服务健康检查是否通过：`docker compose logs <service>`

### 端口冲突

修改 `.env` 中对应的 `*_HOST_PORT` 变量，然后重启：

```bash
docker compose down
docker compose up -d
```

### 数据库连接失败

- 确认 PostgreSQL 容器健康检查通过
- 检查连接字符串中的密码是否与 `.env` 一致
- 检查数据库名称是否正确（每个服务独立数据库）

### RabbitMQ 连接失败

- 等待 RabbitMQ 健康检查通过（首次启动可能需要 30 秒以上）
- 检查用户名密码是否正确

## 部署检查清单

新服务部署前确认：

- [ ] Dockerfile 已添加到 `Api/src/Dockerfile`（复用现有构建模板）
- [ ] `docker-compose.yml` 已添加服务定义
- [ ] 环境变量已添加到 `.env.example`
- [ ] Gateway 路由和集群已配置
- [ ] 数据库迁移已准备
- [ ] 健康检查端点可用
- [ ] 可观测性（OTEL）环境变量已配置
- [ ] 文档已更新
