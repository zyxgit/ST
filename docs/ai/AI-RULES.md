# ST AI 开发规范统一入口

本文是 `docs/ai/` 下所有 **AI 生成强制规则** 的统一入口。AI Agent 在代码生成前应首先阅读本文，再按域定向到对应的细化规则文件。

## 文档体系

| 层级 | 文件 | 用途 |
|------|------|------|
| **统一入口** | `AI-RULES.md`（本文） | 跨域通用规则 + 各域规则导航 |
| 后端规则 | [`api/AI-Rules.md`](./api/AI-Rules.md) | .NET 后端代码生成强制约束 |
| 前端规则 | [`web/AI-Rules.md`](./web/AI-Rules.md) | Vue3 前端代码生成强制约束 |
| 规范索引 | [`README.md`](./README.md) | 完整文档地图 |
| Skill 中心 | [`skills/README.md`](./skills/README.md) | Agent 高密度技能索引 |
| 文档巡检 | [`DOCUMENTATION-AUDIT.md`](./DOCUMENTATION-AUDIT.md) | 当前项目事实、文档缺口与后续同步清单 |

## 项目事实快照

- **Monorepo**：顶层为 `Api/`、`Web/`、`docs/`。
- **后端入口**：`Api/src/ST.slnx`；Aspire 编排入口为 `Api/src/Aspire/ST.Aspire.AppHost`。
- **后端结构**：`Api/src/Microservices/` 下包含 `Identity`、`OperationLog`、`OperationLog.Consumer`、`Test`、`FileUpload`、`Gateway`；`Api/src/ServiceShared/` 与 `Api/src/Infrastructures/` 提供共享启动、认证、异常、日志、EF、Redis、RabbitMQ、Repository、Tasks 等能力。
- **网关路由**：`/api/identity/*`、`/api/operationlog/*`、`/api/test/*`、`/api/files/*` 由 `ST.Gateway` 转发，具体以 `Api/src/Microservices/Gateway/ST.Gateway/appsettings.json` 与 `Program.cs` 为准。
- **前端入口**：`Web/`，技术栈为 Vue 3 + TypeScript + Vite + Pinia + Vue Router + Naive UI + Axios。
- **GitHub 展示入口**：根目录 `README.md`。项目定位、快速开始、能力总览发生变化时必须同步维护。
- **文档巡检记录**：见 [`DOCUMENTATION-AUDIT.md`](./DOCUMENTATION-AUDIT.md)。

## 通用强制规则

### 1. 文档同步

凡新增或变更可交付功能，**必须在同一变更集内**同步更新 `README.md`、`docs/ai/**`（及 `docs/architecture/`、`docs/deploy/`、`docs/database/`、`docs/api/`）中的相关 Markdown。详见 [`common/DocumentationSync.md`](./common/DocumentationSync.md) 与 [`DOCUMENTATION-AUDIT.md`](./DOCUMENTATION-AUDIT.md)。

### 2. 安全底线

- 禁止提交真实密钥、`SigningKey`、连接串到仓库；一律使用占位符 + 环境变量键名。
- 禁止日志打印完整 JWT、refresh token、密码。

### 3. 规范真源

- 架构/命名/约定真源为 `docs/ai/**` 及关联文档。
- **代码级事实以仓库源码为准**；若文档与源码冲突，以源码为准并提示人类更新文档。

### 4. 变更纪律

- 不修改与本任务无关的业务源码。
- 不引入 Git Submodule。
- 不臆造不存在的基类、命名空间、微服务名；生成前先用 `rg` / `find` / IDE 索引确认。

### 5. 任务执行流程

1. 判定域（backend / frontend / gateway / data），打开对应的 `docs/ai/skills/*.skill.md`。
2. 阅读本文及相关域 `AI-Rules.md`。
3. 修改前核对 [`DOCUMENTATION-AUDIT.md`](./DOCUMENTATION-AUDIT.md) 中的当前事实与文档缺口。
4. 输出必须含：变更文件列表 + 将更新的 docs 路径。

## 各域规则快速导航

- [后端 AI 生成规则](./api/AI-Rules.md) — 分层、启动管道、异常、分页、EF、安全、文档同步
- [前端 AI 生成规则](./web/AI-Rules.md) — HTTP、路由、权限、状态、UI、错误处理、文档同步

## 相关工具规则文件

| 工具 | 路径 |
|------|------|
| Claude Code | [`.claude/CLAUDE.md`](../../.claude/CLAUDE.md) |
| GitHub Copilot | [`.github/copilot-instructions.md`](../../.github/copilot-instructions.md) |
| Cursor | [`.cursor/rules/`](../../.cursor/rules/) |
