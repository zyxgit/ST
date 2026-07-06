# ST 文档中心

本文是 ST Monorepo 的文档总入口。文档按“人类理解”和“AI 执行”分层：稳定项目事实写在架构、后端、前端、部署、数据库文档中；AI 执行纪律写在 `docs/ai/`；高密度技能卡写在 `docs/skills/`。

## 文档结构

| 分区 | 说明 |
|------|------|
| [`architecture/`](./architecture/README.md) | 系统架构、服务边界、网关、消息、可观测性 |
| [`backend/`](./backend/README.md) | .NET 后端开发规范、分层、EF、认证、缓存、消息 |
| [`frontend/`](./frontend/README.md) | Vue 前端开发规范、路由、状态、请求、权限、组件 |
| [`devops/`](./devops/README.md) | 本地运行、Docker Compose、CI/CD、可观测性、环境变量 |
| [`database/`](./database/README.md) | PostgreSQL、EF 迁移、Redis 键空间、Outbox/Inbox 表 |
| [`roadmap/`](./roadmap/README.md) | 后续演进路线图，只记录计划与优先级 |
| [`status/`](./status/README.md) | 当前已实现能力与验收入口 |
| [`ai/`](./ai/README.md) | AI Agent 唯一入口、需求合理性审查、任务执行纪律 |
| [`skills/`](./skills/README.md) | AI 高密度技能卡 |

## 真源优先级

1. **源码事实优先**：路径、类型、配置键、路由、数据库表以仓库源码为准。
2. **根 README**：只描述项目定位、快速开始、核心能力和文档地图。
3. **docs 架构/后端/前端/部署/数据库**：描述长期稳定规则和项目事实。
4. **docs/ai**：描述 AI 如何接任务、拆任务、确认风险、同步文档。
5. **docs/skills**：只保留执行提示，不承载完整设计说明。
6. **docs/roadmap**：只写未来计划，不记录大段历史实现流水账。
7. **docs/status**：记录已实现能力和验证入口。

## 文档维护规则

- 新增或变更 API、页面、权限、配置、数据库结构、网关路由、消息事件、任务调度、部署方式时，必须同步更新相关 Markdown。
- 能改已有专题就不要新增零散文档；确需新增时必须在本文件或分区 README 增加链接。
- 文档删除或重构前必须先阅读旧文档、提取仍准确的信息、合并到新真源，再删除旧文件。
- 删除文档后必须用 `rg` 检查旧路径引用，避免断链。
- 文档示例中的路径、类名、配置键必须来自当前仓库或明确标注为示例。

## 推荐阅读顺序

### 新开发者

1. 根目录 `README.md`
2. [`architecture/README.md`](./architecture/README.md)
3. [`backend/README.md`](./backend/README.md) 或 [`frontend/README.md`](./frontend/README.md)
4. [`devops/README.md`](./devops/README.md)
5. [`database/README.md`](./database/README.md)

### AI Agent

1. `AGENTS.md`
2. [`ai/README.md`](./ai/README.md)
3. [`skills/README.md`](./skills/README.md)
4. 与任务相关的后端/前端/架构/部署/数据库文档
