# AI Prompt 规范（项目级）

## 目录

- [目标](#目标)
- [标准提示模板](#标准提示模板)
- [上下文最小集](#上下文最小集)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 目标

让 Codex / Claude / Cursor / Copilot **输出可合并、可审查、与 ST 架构一致**的代码，而非一次性脚本。

## 标准提示模板

```
你是 ST Monorepo 开发者。约束：
- 后端：遵循 docs/ai/api/ 与 Api/src 现有分层（*.Api / *.Application / *.Domain / *.Infra）。
- 前端：遵循 docs/ai/web/ 与 Web/src 现有 axios、Pinia、路由 meta.permission。
- 文档：功能交付须按 docs/ai/common/DocumentationSync.md 同步更新或新增相关 .md（与代码同一变更集）。
- 不引入 Submodule；不提交密钥；异常使用 BusinessException/ProblemDetails 模型。

任务：<一句话需求>

涉及路径：<可选：手动列出文件>
输出要求：给出变更文件列表（含将更新的 docs/**/*.md）+ diff 要点；复杂逻辑附简短理由。
```

## 上下文最小集

| 场景 | 建议附带引用 |
|------|----------------|
| 新 API | `AbstractControllerBase`、`docs/ai/api/DTO.md`、`PagedRequestDto` |
| EF | `InfraModule`、`AddNpgsqlDbContextFromConfig`、`docs/ai/api/EFCore.md` |
| 前端页 | `router/routes.ts`、`stores/auth.ts`、`lib/request.ts` |

## 推荐方案

- 使用 **英文标识符 + 中文注释/用户可见文案**（与现有混合风格可并存时，用户可见字符串保持中文业务语义）。
- 大任务拆解：**先接口契约 → 再实现 → 最后 UI**。

## 禁止事项

- 禁止 Prompt 中粘贴 **真实 JWT、密码、连接串**。
- 禁止要求 AI “顺便重构 unrelated 模块”。

## AI 注意事项

- Agent 每次规划前应 **检索** `docs/ai/` 下对应子文档文件名，避免臆造不存在的基类或路径。
- 若需求与 `GlobalExceptionMiddleware` 行为冲突，以 **仓库代码** 为准并提示人类更新文档。
