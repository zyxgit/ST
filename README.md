# ST Monorepo

ST 是一个面向 **微服务后台管理系统** 的 Monorepo 项目，包含 .NET 后端微服务、YARP 网关、Aspire 本地编排、Vue 3 管理端，以及面向 AI Agent / 团队协作的工程规范文档。

## 项目定位

- **后端微服务**：基于 .NET、ASP.NET Core、EF Core、PostgreSQL、Redis、RabbitMQ、NLog、OpenAPI / Scalar。
- **统一网关**：`ST.Gateway` 使用 YARP 聚合 Identity、OperationLog、Test、FileUpload 等服务。
- **可观测性**：OpenTelemetry (OTLP) → Grafana Alloy → Loki → Grafana 日志链路，支持 LogQL 查询与二阶段规划（Metrics / Tempo）。
- **CI/CD**：GitHub Actions 自动构建镜像 → GHCR → 部署、EF Core 迁移、数据种子、健康检查、镜像清理。
- **前端管理端**：`Web/` 使用 Vue 3、TypeScript、Vite、Pinia、Vue Router、Naive UI、Axios。
- **工程化文档**：`docs/ai/AI-RULES.md` 是 AI 生成规则总入口，要求功能变更与文档同步提交。

## 仓库结构

```text
ST/
├── Api/                      # .NET 解决方案、微服务、共享库、基础设施
│   ├── config/               # 环境变量示例
│   ├── src/
│   │   ├── Aspire/           # Aspire AppHost 与 ServiceDefaults
│   │   ├── Infrastructures/  # EF、Redis、EventBus、Repository、Tasks 等基础设施
│   │   ├── Microservices/    # Identity、OperationLog、Test、FileUpload、Gateway
│   │   ├── ServiceShared/    # 共享 WebApi、Application、Domain、公共原语
│   │   └── ST.slnx           # 后端解决方案入口
├── Web/                      # Vue 3 + TypeScript 管理端
├── deploy/                   # Docker Compose、Alloy/Loki/Grafana 配置、环境变量
├── .github/workflows/        # CI/CD：构建、部署、迁移、清理
└── docs/                     # 架构、API、数据库、部署、AI 协作规范
```

## 后端能力

| 模块 | 路径 | 说明 |
|------|------|------|
| Aspire | `Api/src/Aspire/ST.Aspire.AppHost` | 本地编排 Redis、PostgreSQL、RabbitMQ 与各微服务 |
| Gateway | `Api/src/Microservices/Gateway/ST.Gateway` | YARP 反向代理、CORS、限流、文档入口 |
| Identity | `Api/src/Microservices/Identity` | 用户、角色、菜单、权限、JWT 登录、修改密码 |
| OperationLog | `Api/src/Microservices/OperationLog` | 操作日志 API 与消费者 |
| FileUpload | `Api/src/Microservices/FileUpload` | 文件上传、元数据管理、本地存储扩展点 |
| Test | `Api/src/Microservices/Test` | 示例微服务与分层模板 |
| Shared | `Api/src/ServiceShared` | 统一异常、认证授权、请求日志、模块化启动、公共 DTO |
| Infrastructures | `Api/src/Infrastructures` | EF Core、PostgreSQL、Redis、RabbitMQ、Repository、后台任务等 |

## 前端能力

| 模块 | 路径 | 说明 |
|------|------|------|
| 入口 | `Web/src/main.ts` | Vue 应用启动 |
| 路由 | `Web/src/router/` | 登录、后台页面、权限路由 |
| 状态 | `Web/src/stores/` | Pinia 会话、菜单、应用状态 |
| 请求 | `Web/src/lib/request.ts` | Axios 基址、Bearer Token、401 刷新处理 |
| 页面 | `Web/src/views/` | 登录、仪表盘、用户、角色、菜单、操作日志等 |
| 组件 | `Web/src/components/` | 布局、通用表格操作、富文本、图标选择、头像裁剪、修改密码等 |

## 网关路由速览

| 外部路径 | 下游服务 |
|----------|----------|
| `/api/identity/*` | Identity |
| `/api/operationlog/*` | OperationLog |
| `/api/test/*` | Test |
| `/api/files/*` | FileUpload |
| `/docs/{service}/*` | 对应服务 Scalar / OpenAPI 文档 |

具体配置以 `Api/src/Microservices/Gateway/ST.Gateway/appsettings.json` 和 `Program.cs` 为准。

## 快速开始

### 环境要求

- .NET SDK（与项目目标框架保持一致）
- Docker / Docker Compose（用于 Aspire 编排 PostgreSQL、Redis、RabbitMQ）
- Node.js `^20.19.0 || >=22.12.0`
- pnpm

### 后端构建

```bash
dotnet build Api/src/ST.slnx
```

### Aspire 启动后端服务

```bash
dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj
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

### 启动可观测性栈

```bash
cd deploy
docker compose up -d alloy loki grafana
```

详见 [docs/deploy/README.md](docs/deploy/README.md)（含 OTLP → Alloy → Loki → Grafana 日志链路）。

## 文档入口

| 文档 | 用途 |
|------|------|
| `docs/ai/AI-RULES.md` | AI Agent 必读总入口 |
| `docs/ai/DOCUMENTATION-AUDIT.md` | 文档巡检、遗漏与后续优化清单 |
| `docs/architecture/README.md` | 架构导航 |
| `docs/api/README.md` | 对外 API 与集成说明 |
| `docs/database/README.md` | 数据与存储导航 |
| `docs/deploy/README.md` | 部署与运行导航（含 Docker 镜像、CI/CD、OTel 可观测性栈） |
| `docs/ai/common/Observability.md` | OpenTelemetry → Alloy → Loki → Grafana 日志链路 |
| `docs/ai/common/DocumentationSync.md` | 功能迭代与文档同步规则 |

## 文档同步要求

后续凡是新增或调整 API、页面、权限、配置、数据库结构、网关路由、任务调度、上传存储、部署方式等可交付能力，必须在同一变更集中同步更新相关 Markdown 文档。至少检查：

- `README.md`
- `docs/ai/AI-RULES.md`
- `docs/ai/DOCUMENTATION-AUDIT.md`
- `docs/ai/**`
- `docs/architecture/README.md`
- `docs/api/README.md`
- `docs/database/README.md`
- `docs/deploy/README.md`

## AI / 协作规范

本项目内置面向 AI Agent 的规范体系：

1. 先读 `docs/ai/AI-RULES.md`。
2. 按任务域继续读取 `docs/ai/api/AI-Rules.md`、`docs/ai/web/AI-Rules.md` 或 `docs/ai/skills/*.skill.md`。
3. 生成或修改代码前先核对仓库真实路径与现有类型。
4. 代码变更必须同步文档，避免文档与源码漂移。

## 安全说明

- 不提交真实密钥、JWT SigningKey、数据库连接串、SMTP 密码、对象存储 AccessKey。
- 本地或生产配置应通过环境变量、UserSecrets、密钥管理器或部署平台注入。
- 日志中禁止输出完整 JWT、Refresh Token、密码等敏感信息。

## License

版权所有，仅供学习参考，未经许可不得用于商业用途。



