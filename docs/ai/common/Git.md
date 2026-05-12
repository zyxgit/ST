# Git 与协作规范（ST Monorepo）

## 目录

- [仓库形态](#仓库形态)
- [分支策略](#分支策略)
- [Commit 消息](#commit-消息)
- [忽略与换行](#忽略与换行)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 仓库形态

- **单一 Git 根目录**在 Monorepo 根（`ST/`），**不使用 Submodule**。
- 子目录仅为 `Api/`、`Web/` 等普通文件夹，历史上若有独立 `.git` 已移除。

## 分支策略

| 分支 | 用途 |
|------|------|
| `main`（或 `master`） | 可发布主线；合并前 CI 通过 |
| `feat/<scope>-<topic>` | 功能开发，例如 `feat/api-user-search` |
| `fix/<topic>` | 缺陷修复 |
| `chore/<topic>` | 工具、文档、构建无关业务行为的变更 |

**推荐**：一次 PR 聚焦单一主题；大功能拆分子 PR（Api / Web / docs 可同 PR 若强相关）。

## Commit 消息

采用简洁前缀 + 描述：

```
<type>(<scope>): <subject>
```

| type | 含义 |
|------|------|
| `feat` | 新功能 |
| `fix` | 修复 |
| `docs` | 文档 |
| `chore` | 杂项、构建、脚本 |
| `refactor` | 重构（无行为变更） |
| `test` | 测试 |

示例：

```
feat(api): add user search endpoint with paging
fix(web): handle 401 refresh race on login
docs(ai): add Redis caching guidelines
```

## 忽略与换行

- 根目录 `.gitignore` 合并了前后端忽略规则；**勿**在子项目再引入会忽略 `*.sln` 的全局规则。
- `.gitattributes` 约定文本 **CRLF**、shell 脚本 **LF**；提交前避免混用导致噪音 diff。

## 推荐方案

- 机密使用 **环境变量 / UserSecrets**，勿提交 `appsettings.Production.json` 中的密钥。
- 大文件与构建产物依赖 `.gitignore`，不强制提交 `bin/`、`obj/`、`Web/dist/`。

## 禁止事项

- 禁止提交 **云服务密钥、连接串、JWT SigningKey、私钥**。
- 禁止 `--force` 推送共享主线（除非团队流程允许且已沟通）。
- 禁止将 `.claude/settings.local.json` 等机敏本地配置作为“示例”复制进文档真实值。

## AI 注意事项

- AI **不得**生成包含占位真实密钥的 `appsettings`；一律使用占位符并指向环境变量键名（如 `Jwt__SigningKey`）。
- 变更若涉及公共契约（路由、DTO），Commit 信息应标明 `api` 或 `web` scope。
