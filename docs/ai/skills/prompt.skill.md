# prompt.skill

## 1. Skill Name

`st-prompt-agent` — 面向 Agent 的提示与交付约束（与文档同步强制）。

## 2. Purpose

- 固定任务拆解、上下文引用、输出格式；强制 **DocumentationSync** 与 Skill 索引，减少漂移与臆造类型。

## 3. Tech Stack

| 项 | 说明 |
|----|------|
| 规范真源 | `docs/ai/**`、`docs/ai/skills/*.skill.md` |
| 同步规则 | `docs/ai/common/DocumentationSync.md` |
| 工具 | `.cursor/rules/*.mdc`、`.claude/CLAUDE.md`、`.github/copilot-instructions.md` |

## 4. Architecture Rules

- 任务前先判定域：**backend** / **frontend** / **gateway** / **data**，打开对应 `*.skill.md` + 深层 md。
- 输出必须含：**变更文件列表** + **将更新的 docs 路径**（功能交付）。

## 5. Coding Rules

- 标准开场引用：`docs/ai/common/Prompt.md` 模板 + Skill Name。
- 禁止在提示中粘贴真实 JWT、连接串、SigningKey（占位 + 环境变量键名）。

## 6. Naming Rules

- Commit：`feat(api):`、`fix(web):`、`docs(ai):`（见 `docs/ai/common/Git.md`）。

## 7. Best Practices

- 复杂任务：**契约（DTO/OpenAPI）→ 后端 → 前端类型/API → 页面**。
- 检索：设计前先 `glob`/`grep` 现有 Controller、Dto、`PermissionCode`。

## 8. Forbidden Practices

- “顺便重构”无关模块。
- 无检索凭空创建 `ST.Fake.*` 命名空间。
- 仅输出代码不列文档更新（功能可见变更时）。

## 9. AI Generation Constraints

- 响应末尾列出：`Skill used: backend.skill + auth.skill`（示例）与 `Docs touched: docs/ai/api/Auth.md`.
- 若需求与 `GlobalExceptionMiddleware` 行为冲突，以 **源码为准** 并提示人类更新文档。

## 10. Example Code

```
你是 ST Monorepo 开发者。
必读：docs/ai/skills/backend.skill.md、docs/ai/skills/auth.skill.md
约束：docs/ai/common/DocumentationSync.md

任务：为 Identity 服务增加用户导出 CSV 接口（管理员）。
输出：
1) 将修改/新增的文件路径列表（含 docs）
2) 关键代码片段或 diff 要点
3) EF 迁移命令（若有）

禁止：真实密钥、Submodule。
```

## 11. Related Documents

- `docs/ai/common/Prompt.md`、`DocumentationSync.md`
- `docs/ai/README.md`
- `docs/ai/skills/README.md`
