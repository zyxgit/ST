# 功能迭代与文档同步（必读）

凡 **新增或变更可交付功能**（API、页面、权限、配置、数据模型、网关、任务调度等），**同一变更集内**须同步更新文档，避免 `docs/ai` 与源码漂移。AI Agent 与人类开发者均适用。

## 目录

- [总原则](#总原则)
- [按变更类型对照表](#按变更类型对照表)
- [应更新或新增的 Markdown](#应更新或新增的-markdown)
- [提交与审查](#提交与审查)
- [禁止事项](#禁止事项)

## 总原则

1. **代码与文档同一 PR / 同一提交系列**：除非紧急热修，否则不单独「先合代码、后补文档」。
2. **优先改现有专题**：能在 `docs/ai/api/*.md`、`docs/ai/web/*.md`、`docs/ai/common/*.md` 说清楚的，不新建零散文件；若变更影响 **Agent 高密度规则摘要**，同步对应 `docs/ai/skills/*.skill.md` 与 [`skills/README.md`](../skills/README.md)。
3. **确需新文件时**：放在 `docs/ai/` 对应分区（`common/` / `api/` / `web/`），命名 **PascalCase**，并在 [`../README.md`](../README.md) 或本分区内 `README.md` **增加链接**。
4. **人类速览与 AI 深读分工**：
   - 架构/目录级变更：同步 `docs/architecture/README.md`；启动链等详情同步 `docs/ai/common/Architecture.md`。
   - 运行与部署：同步 `docs/deploy/README.md`。
   - 存储与迁移：同步 `docs/database/README.md`。
   - 对外集成：同步 `docs/api/README.md`。

## 按变更类型对照表

| 变更类型 | 至少更新的文档 |
|----------|----------------|
| 新微服务或新 bounded context | `docs/architecture/README.md`；`docs/ai/api/README.md`（索引说明）；`docs/deploy/README.md`（若启动方式变化）；**`docs/ai/api/ServiceTemplate.md` 的 Aspire 编排节** 确保已有注册指南；Aspire AppHost 的 `AppHost.cs` + `csproj`；新服务可增 `docs/ai/api/<Topic>.md` 若现有专题无法覆盖 |
| 新 Controller / 路由 / DTO / 错误码 | `docs/ai/api/DTO.md`、`Exception.md` 或 `Result.md` 中**补充小节或示例**；对外路径变更则 `docs/api/README.md` |
| EF 实体 / 迁移 / 新 DbContext 模式 | `docs/ai/api/EFCore.md`、`Repository.md`；`docs/database/README.md` |
| 新 Redis 键空间或缓存策略 | `docs/ai/api/Redis.md`、`docs/ai/common/Cache.md` |
| JWT / 权限 / Policy 命名变化 | `docs/ai/api/Auth.md`；`docs/ai/web/Permission.md` |
| 网关 YARP / 下游 / 限流 | `docs/ai/api` 中可增专题或扩展现有架构说明；**必须** `docs/deploy/README.md` |
| Hangfire / 后台任务约定 | `docs/ai/api/Hangfire.md` |
| 新前端页面 / 路由 / 权限码 | `docs/ai/web/Router.md`、`Permission.md`；`DynamicRoute.md`（若菜单与路由策略变化） |
| 新 `VITE_*` 或代理 | `docs/ai/web/Env.md`、`docs/deploy/README.md` |
| 新 Axios 行为（拦截器、刷新） | `docs/ai/web/Request.md` |
| Git / Monorepo 流程变化 | `docs/ai/common/Git.md`、`Monorepo.md` |
| 多租户 / SaaS 策略落地 | `docs/ai/common/MultiTenant.md` 及关联 `api` 数据文档 |
| 横切规则变化（鉴权/缓存/日志等全局约束） | 对应专题 md + **`docs/ai/skills/*.skill.md`**（如 `auth.skill.md`、`cache.skill.md`） |

## 应更新或新增的 Markdown

**最小检查清单**（交付前勾选）：

- [ ] 是否已有对应 `docs/ai/**` 专题？→ **编辑补段**（含标题层级、目录、示例、推荐/禁止、AI 注意）。
- [ ] 是否引入新环境变量或密钥名？→ `docs/ai/web/Env.md` 或 `docs/ai/api/ServiceTemplate.md` / `docs/deploy/README.md`。
- [ ] 是否影响「如何构建/运行」？→ `docs/deploy/README.md`。
- [ ] 是否影响架构鸟瞰？→ `docs/architecture/README.md`。
- [ ] AI 工具是否需新约束？→ `docs/ai/AI-RULES.md`（统一入口）→ 再按域同步 `docs/ai/api/AI-Rules.md` / `docs/ai/web/AI-Rules.md`、`.cursor/rules/`、`.claude/CLAUDE.md`、`.github/copilot-instructions.md`（仅当规则级变化时）。

## 提交与审查

- Commit message 建议带 `docs:` 子提交或在同一条 `feat` 中列出「文档已更新」。
- Code Review 对功能 PR 默认要求：**可见的文档 diff** 或与任务说明中引用「无需文档」的豁免理由。

## 禁止事项

- 禁止仅写个人笔记路径而不更新 `docs/ai`（除非团队明确排除该功能）。
- 禁止新增 **空文档** 或仅标题无落地示例的占位文件。
- 禁止文档与代码中的 **路径、类名、配置键** 不一致。

## AI 注意事项

完成任务前执行：

1. 对照上表列出「将修改的 `.md` 列表」。
2. 对每个文件执行补全：目录、规范说明、**真实代码示例**（来自本仓库或等价可编译片段）。
3. 若无法确定归属，**先更新** `docs/ai/README.md` 地图并新增专题链接。
