# ST 文档巡检与优化清单

本文记录基于当前仓库结构的文档巡检结论，供后续 AI Agent 与团队成员在新增或调整功能时同步维护。统一入口仍为 [`AI-RULES.md`](./AI-RULES.md)。

## 当前项目事实

| 领域 | 当前事实 |
|------|----------|
| Monorepo | 顶层包含 `Api/`、`Web/`、`docs/`，未采用 Git Submodule |
| 后端入口 | `Api/src/ST.slnx` |
| 本地编排 | `Api/src/Aspire/ST.Aspire.AppHost`，编排 Redis、PostgreSQL、RabbitMQ、Gateway 与各微服务 |
| 网关 | `Api/src/Microservices/Gateway/ST.Gateway`，使用 YARP、CORS、限流与 Scalar/OpenAPI 文档跳转 |
| 微服务 | `Identity`、`OperationLog`、`OperationLog.Consumer`、`Test`、`FileUpload` |
| 共享库 | `Api/src/ServiceShared/` 提供共享 WebApi、Application、Domain、认证、异常、日志、模块化启动等能力 |
| 基础设施 | `Api/src/Infrastructures/` 提供 EF Core、PostgreSQL、Redis、RabbitMQ EventBus、Repository、Tasks、Email、**ReliableMessaging（Outbox / Inbox）** 等 |
| 前端 | `Web/` 为 Vue 3 + TypeScript + Vite + Pinia + Vue Router + Naive UI 管理端 |
| AI 文档入口 | `docs/ai/AI-RULES.md` |

## 本次发现的优化点

| 类型 | 发现 | 本次处理 |
|------|------|----------|
| GitHub 展示 | 根目录缺少面向 GitHub 首页展示的 `README.md` | 新增根目录 `README.md`，补充项目定位、结构、能力、快速开始、文档入口和安全说明 |
| AI 入口 | `docs/ai/AI-RULES.md` 已有强制规则，但缺少项目事实快照与文档巡检入口 | 补充项目事实快照、文档巡检入口、文档同步范围与 `rg` 核验要求 |
| API 导航 | `docs/api/README.md` 的服务列表未完整强调 FileUpload、OperationLog.Consumer 与文档路由 | 更新服务列表和网关路由说明 |
| 文档同步 | 现有规则强调 `docs/ai/**` 等目录，但 GitHub 展示页也会随功能变化而过期 | 将根目录 `README.md` 纳入后续同步检查范围 |
| 前端介绍 | `Web/README.md` 仍偏 Vite 模板说明 | 后续可改为 Web 子项目专用说明，避免与根 README 重复 |
| 运行说明 | 部署文档已有后端、Aspire、前端命令，但缺少面向初次访问者的总览 | 根 README 汇总快速开始；后续若加入 Docker Compose/K8s/Helm，应同步部署文档 |
| Aspire 机密 | AppHost 使用 `builder.AddParameter()` 管理密码，缺少用户机密初始化与修改的说明 | `docs/deploy/README.md` 新增「Aspire 用户机密管理」小节 |

## 后续新增或调整必须同步的文档

凡新增或调整以下内容，必须在同一 PR / 同一提交系列同步更新文档：

- API、DTO、错误格式、认证授权、权限码。
- 前端页面、路由、菜单、状态、请求封装、环境变量。
- 数据模型、EF 迁移、数据库提供方、缓存键、消息队列、任务调度。
- 网关路由、服务端口、CORS、限流、OpenAPI / Scalar 文档入口。
- 文件上传策略、本地/对象存储配置、大小限制、安全校验。
- 部署方式、启动命令、环境变量、密钥注入方式。
- AI 规则、Skill、代码生成约束、目录结构约定。

最少检查并按需更新：

1. `README.md`
2. `docs/ai/AI-RULES.md`
3. `docs/ai/DOCUMENTATION-AUDIT.md`
4. `docs/ai/**`
5. `docs/architecture/README.md`
6. `docs/api/README.md`
7. `docs/database/README.md`
8. `docs/deploy/README.md`
9. 对应子项目 README（例如 `Web/README.md`）

## 文档维护原则

- **源码优先**：若文档与源码冲突，以源码为准，并在同一变更中修正文档。
- **入口清晰**：面向 GitHub 展示的内容放在根 `README.md`；面向 AI 规则的内容放在 `docs/ai/`；面向运行部署的内容放在 `docs/deploy/`。
- **避免复制漂移**：重复信息只保留摘要，并链接到真源；端口、路由、配置键等易变信息应标注源码位置。
- **先核验再生成**：新增规则或示例前使用 `rg`、`find`、`dotnet sln/list`、`pnpm` 等方式核对真实路径与命名。
- **安全默认**：所有示例密钥、连接串、Token 均使用占位符，不写入真实敏感值。

## 建议后续迭代

1. 将 `Web/README.md` 从 Vite 默认模板升级为 ST Web 子项目说明。
2. 为每个微服务补充简短服务说明与核心接口索引，必要时放入 `docs/api/services/`。
3. 为 Aspire、本地容器、数据库迁移、初始化种子数据建立端到端启动手册。
4. 若仓库公开发布，补充 `LICENSE`、贡献指南和安全披露说明。
5. 若引入 CI/CD，新增 `docs/deploy/ci-cd.md` 并在根 README 与部署 README 链接。
