# ST Monorepo

ST 是一个企业级微服务后台管理系统模板，采用 **.NET 微服务 + YARP Gateway + Aspire + PostgreSQL + Redis + RabbitMQ + Vue 3**。仓库同时提供面向人类和 AI Agent 的工程规范文档，便于持续演进高并发、可靠消息、SaaS、多租户、文件中心和可观测性能力。

🔗 **预览地址**：[https://st-template.xyz](https://st-template.xyz)

## 项目定位

- **后端微服务**：ASP.NET Core、EF Core、PostgreSQL、Redis、RabbitMQ、OpenTelemetry、NLog、OpenAPI/Scalar。
- **统一网关**：`ST.Gateway` 基于 YARP 提供反向代理、CORS、ForwardedHeaders、限流和文档入口。
- **跨服务事务样板**：Order、Inventory、Payment 通过 RabbitMQ、Outbox/Inbox、Saga 状态表实现最终一致性。
- **文件中心**：FileUpload 提供上传、下载、公开下载、签名下载、分片上传、断点续传与秒传查询入口。
- **SaaS 基础**：Identity 中包含租户、租户用户、租户配额，多业务表预留 `tenant_id`。
- **前端管理端**：`Web/` 使用 Vue 3、TypeScript、Vite、Pinia、Vue Router、Naive UI、Axios。

## 仓库结构

```text
ST/
├── Api/                      # .NET 解决方案、微服务、共享库、基础设施
│   ├── config/               # 环境配置示例
│   └── src/
│       ├── Aspire/           # Aspire AppHost 与 ServiceDefaults
│       ├── Infrastructures/  # EF、Redis、EventBus、ReliableMessaging、Repository、Tasks
│       ├── Microservices/    # Identity、OperationLog、FileUpload、Test、Order、Inventory、Payment、Gateway
│       ├── ServiceShared/    # 共享 WebApi、Application、Domain、认证、异常、日志、公共 DTO
│       └── ST.slnx           # 后端解决方案入口
├── Web/                      # Vue 3 + TypeScript 管理端
├── deploy/                   # Docker Compose、Alloy、Loki、Grafana、Prometheus 配置
├── docs/                     # 标准化项目文档
└── AGENTS.md                 # Codex/Agent 仓库级指令
```

## 后端能力

| 模块 | 路径 | 说明 |
|------|------|------|
| Aspire | `Api/src/Aspire/ST.Aspire.AppHost` | 本地编排 PostgreSQL、Redis、RabbitMQ 与各微服务 |
| Gateway | `Api/src/Microservices/Gateway/ST.Gateway` | YARP 反向代理、CORS、限流、文档入口 |
| Identity | `Api/src/Microservices/Identity` | 用户、角色、菜单、权限、JWT、RefreshToken、租户与配额 |
| OperationLog | `Api/src/Microservices/OperationLog` | 操作日志 API、异步消费者、死信查询与重放 |
| FileUpload | `Api/src/Microservices/FileUpload` | 文件上传、下载、签名 URL、分片上传 |
| Test | `Api/src/Microservices/Test` | 示例服务、可靠消息测试接口、分层模板 |
| Order | `Api/src/Microservices/Order` | 订单创建、查询、取消、Saga 状态、超时关单 |
| Inventory | `Api/src/Microservices/Inventory` | SKU、库存增加、Redis Lua 预扣、DB 乐观锁兜底 |
| Payment | `Api/src/Microservices/Payment` | 模拟支付成功/失败、支付记录查询 |
| Shared | `Api/src/ServiceShared` | 共享异常、认证授权、请求日志、模块化启动、公共 DTO |
| Infrastructures | `Api/src/Infrastructures` | EF Core、PostgreSQL、Redis、RabbitMQ、ReliableMessaging、Repository、后台任务 |

## 网关路由速览

| 外部路径 | 下游服务 |
|----------|----------|
| `/api/identity/*`、`/identity/*` | Identity |
| `/api/operationlog/*`、`/operationlog/*` | OperationLog |
| `/api/test/*`、`/test/*` | Test |
| `/api/files/*` | FileUpload |
| `/api/orders/*`、`/orders/*` | Order |
| `/api/inventory/*` | Inventory |
| `/api/payments/*` | Payment |
| `/docs/{service}/*` | 对应服务 Scalar/OpenAPI 文档 |

> 具体以 `Api/src/Microservices/Gateway/ST.Gateway/appsettings.json` 的 `ReverseProxy` 配置为准。

## 前端能力

| 模块 | 路径 | 说明 |
|------|------|------|
| 入口 | `Web/src/main.ts` | Vue 应用启动 |
| 路由 | `Web/src/router/` | 登录、后台页面、权限路由 |
| 状态 | `Web/src/stores/` | Pinia 会话、菜单、应用状态 |
| 请求 | `Web/src/lib/request.ts` | Axios 基址、Bearer Token、401 刷新处理 |
| API | `Web/src/api/` | 按业务域拆分 API 调用 |
| 页面 | `Web/src/views/` | 登录、仪表盘、用户、角色、菜单、操作日志等 |
| 组件 | `Web/src/components/` | 布局、表格操作、图标、头像裁剪、富文本等 |

## 快速开始

### 环境要求

- .NET SDK（与项目目标框架保持一致）
- Docker / Docker Compose
- Node.js `^20.19.0 || >=22.12.0`
- pnpm

### 后端构建

```bash
dotnet build Api/src/ST.slnx
```

### Aspire 启动后端

```bash
dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj
```

### Docker Compose 启动

```bash
cd deploy
docker compose up -d
```

### 前端开发

```bash
cd Web
pnpm install
pnpm dev
```

### 前端构建

```bash
cd Web
pnpm build
```

## CI/CD

### GitHub Actions 工作流

| 工作流 | 文件 | 触发方式 | 说明 |
|--------|------|----------|------|
| Build Docker Images | `.github/workflows/build-images.yml` | 手动触发 | 并行构建 10 个服务镜像，推送至 GHCR |
| Deploy to Server | `.github/workflows/deploy.yml` | 构建完成后自动 / 手动触发 | 拉取镜像、迁移数据库、启动服务 |

### 镜像构建

- **后端**：9 个微服务并行构建，使用统一 Dockerfile（多阶段构建，SDK 10.0 编译 + ASP.NET 运行时）
- **前端**：Node 22 + pnpm 构建，Nginx Alpine 托管静态文件
- **镜像仓库**：GitHub Container Registry (`ghcr.io`)
- **构建缓存**：GitHub Actions Cache (`type=gha`)

### 镜像标签策略

| 标签 | 说明 |
|------|------|
| `latest` | main 分支最新构建 |
| `YYYYMMDD-<run>` | 日期 + 运行号 |
| `dev-YYYYMMDD-<run>` | 开发标签，自动清理旧版本 |
| `sha-<short>` | Git commit SHA |
| `<branch>` | 分支名 |

### 部署流程

```text
构建完成 → 自托管 Runner 拉取代码 → 登录 GHCR → 拉取镜像
  → 启动基础设施 (PostgreSQL/Redis/RabbitMQ)
  → 等待健康检查通过
  → 执行数据库迁移
  → 启动全部服务
  → 验证容器状态
```

### 自托管 Runner

- 部署使用 GitHub Actions self-hosted runner
- 支持 2C2G 低配服务器（`docker-compose.2c2g.yml`）
- 部署目录：`/home/admin/st/deploy`

### 镜像清理

自动清理旧的 `dev-*` 标签镜像，每个服务保留最近 4 个版本，支持 dry-run 模式预览。

## 文档入口

| 文档 | 用途 |
|------|------|
| `docs/README.md` | 文档总入口 |
| `docs/architecture/README.md` | 架构、服务边界、网关、消息与可观测性 |
| `docs/backend/README.md` | 后端开发规范 |
| `docs/frontend/README.md` | 前端开发规范 |
| `docs/devops/README.md` | 本地运行、Docker、CI/CD、可观测性 |
| `docs/database/README.md` | 数据库、迁移、缓存、可靠消息表 |
| `docs/roadmap/README.md` | 后续路线图 |
| `docs/status/README.md` | 当前已实现能力 |
| `docs/ai/README.md` | AI Agent 唯一入口与执行纪律 |
| `docs/skills/README.md` | AI 高密度技能卡索引 |

## License

版权所有，仅供学习参考，未经许可不得用于商业用途。
