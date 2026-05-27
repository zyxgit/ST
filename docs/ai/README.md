# ST AI 工程化规范中心

本目录为 **ST Monorepo**（`Api/` .NET + `Web/` Vue）的统一 AI 与协作规范入口，适用于 Codex、Claude、Cursor、Copilot 及各类 Agent。

## 文档地图

| 分区 | 路径 | 用途 |
|------|------|------|
| 通用 | [`common/`](./common/) | Git、Monorepo、命名、日志、缓存、多租户预留、Prompt、可观测性、**[功能迭代文档同步](./common/DocumentationSync.md)** |
| 后端 | [`api/`](./api/) | DDD 分层、EF Core、JWT、网关、Hangfire、DTO/异常/Result |
| 前端 | [`web/`](./web/) | Vue3、Pinia、路由权限、请求封装、组件与样式 |
| AI 规则 | [`AI-RULES.md`](./AI-RULES.md) | **AI 生成强制规则统一入口**，聚合各域规则 |
| 文档巡检 | [`DOCUMENTATION-AUDIT.md`](./DOCUMENTATION-AUDIT.md) | 当前仓库事实、文档缺口、后续同步清单 |
| Agent Skill | [`skills/`](./skills/) | **ST AI Skill Center**：高密度 `*.skill.md`（后端/前端/架构等） |

## 项目事实摘要（与仓库一致）

- **后端**：解决方案入口 `Api/src/ST.slnx`；Aspire `Api/src/Aspire/ST.Aspire.AppHost`；网关 `Api/src/Microservices/Gateway/ST.Gateway`（YARP + `ReverseProxy` 配置）。
- **微服务**：Identity（用户/角色/权限）、OperationLog（操作日志）、Test（示例）、**FileUpload**（文件上传与管理，本地存储 + IFileStorageService 可扩展接口）。
- **网关路由**：`/api/identity/*` → Identity、`/api/operationlog/*` → OperationLog、`/api/test/*` → Test、**`/api/files/*` → FileUpload**；Docs 入口 `/docs/服务名/scalar/v1`。
- **共享启动**：`AddSharedWebApi` / `UseSharedWebApi`（`ST.Shared.WebApi`），全局异常 `GlobalExceptionMiddleware`，JWT + `perm:` 权限策略。
- **前端**：`Web/` Vite + Vue3 + TypeScript；Axios 封装 `Web/src/lib/request.ts`；Pinia `Web/src/stores/auth.ts`；路由守卫与菜单树 bootstrap。

## 阅读顺序（新人 / AI）

1. [`common/Monorepo.md`](./common/Monorepo.md) → [`common/Git.md`](./common/Git.md)
2. [`common/Architecture.md`](./common/Architecture.md) + [`api/README.md`](./api/README.md) 或 [`web/README.md`](./web/README.md)
3. 按任务深入：EF/Redis/Auth 等子文档；Agent 优先加载 [`skills/README.md`](./skills/README.md)

## 阅读入口

AI Agent **首先阅读** [`AI-RULES.md`](./AI-RULES.md)（通用规则 + 各域导航），再核对 [`DOCUMENTATION-AUDIT.md`](./DOCUMENTATION-AUDIT.md) 的项目事实与文档缺口，最后按域阅读 `api/AI-Rules.md` 或 `web/AI-Rules.md`。

## 与根目录规范联动

- **Claude Code**：根目录 [`.claude/CLAUDE.md`](../../.claude/CLAUDE.md)
- **GitHub Copilot**：[`.github/copilot-instructions.md`](../../.github/copilot-instructions.md)
- **Cursor**：[`.cursor/rules/`](../../.cursor/rules/)

## AI 使用约束（总览）

- 以本目录为真源；修改业务代码前须对照 `api/` 与 `web/` 中的**禁止事项**与**真实类型/文件路径**。
- **功能新增/变更须同步更新 Markdown**（同一变更集内）：见 [`common/DocumentationSync.md`](./common/DocumentationSync.md)，并将根目录 `README.md` 与 [`DOCUMENTATION-AUDIT.md`](./DOCUMENTATION-AUDIT.md) 纳入检查。
- 不引入 Submodule；不移动 `Api` / `Web` 顶层结构；不提交密钥与本地环境文件（见 `common/Git.md`）。

## 相关导航

- 人类可读的架构速览：[`../architecture/README.md`](../architecture/README.md)
- 部署与运行：[`../deploy/README.md`](../deploy/README.md)
- 数据与存储：[`../database/README.md`](../database/README.md)
- 对外 API 说明入口：[`../api/README.md`](../api/README.md)
