# ST Monorepo — Claude Code 项目说明

你是 ST 仓库的编码助手。本文件与 `docs/ai/` 下规范**一致**；冲突时以 **仓库源码** 与 **`docs/ai`** 为准。

## 布局

- **后端**：`Api/`，主解决方案 `Api/src/ST.slnx`；Aspire `Api/src/Aspire/`；网关 `Api/src/Microservices/Gateway/ST.Gateway`（YARP）。
- **前端**：`Web/`，Vite + Vue3 + TypeScript + Pinia + Axios。
- **规范真源**：`docs/ai/README.md`（索引）、`docs/ai/common/`、`docs/ai/api/`、`docs/ai/web/`、`docs/ai/skills/`（`*.skill.md` Agent 高密度索引）。路线图开发优先阅读 `docs/ai/common/AgentExecutionGuide.md` 与 `docs/ai/common/DevelopmentRoadmap.md`。

## 硬约束

1. **不修改**与本任务无关的业务源码；不引入 Git Submodule。
2. **不生成**含真实密钥、`SigningKey`、生产连接串的配置；一律用占位符 + 环境变量键名（见 `docs/ai/api/ServiceTemplate.md`）。
3. 后端异常：**`BusinessException` / `DomainException`**；全局输出 **`application/problem+json`**（`GlobalExceptionMiddleware`）。
4. 后端分页：**`PagedRequestDto` / `PagedResultDto<T>`**。
5. 前端 HTTP：**仅** `Web/src/lib/request.ts`；权限：**`PermissionCode`** + 路由 `meta.permission`。
6. 新建微服务/页面须遵循分层与 `Program.cs` 模板（`AddSharedWebApi` / `UseSharedWebApi`）。
7. **文档同步**：任何可交付功能变更须在同一任务中完成 `docs/ai`（及 `docs/architecture`、`docs/deploy`、`docs/database`、`docs/api` 等索引）的**补充或新增**，细则见 `docs/ai/common/DocumentationSync.md`。

## 任务流程

1. 阅读 `docs/ai` 中与任务匹配的条目（如 EF → `docs/ai/api/EFCore.md`，路由 → `docs/ai/web/Router.md`）。
2. 用 `grep`/`glob` 确认是否已有同类类型或控制器，避免重复抽象。
3. 给出变更文件列表；API 变更同步前端类型与 `api/*.ts`。
4. 按 `DocumentationSync.md` 列出并编辑将更新的 `.md` 文件，保证示例与路径与仓库一致。

## 文档导航

| 场景 | 文档 |
|------|------|
| Git / Monorepo | `docs/ai/common/Git.md`、`Monorepo.md` |
| 后端分层 | `docs/ai/api/Application.md`、`Domain.md` |
| JWT / 权限 | `docs/ai/api/Auth.md`、`docs/ai/web/Permission.md` |
| Axios / 401 | `docs/ai/web/Request.md` |
| 功能迭代写文档 | `docs/ai/common/DocumentationSync.md` |

## 其他工具对齐

- Cursor：`.cursor/rules/*.mdc`
- Copilot：`.github/copilot-instructions.md`
