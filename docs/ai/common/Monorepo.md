# Monorepo 规范（ST）

## 目录

- [目录契约](#目录契约)
- [构建与依赖](#构建与依赖)
- [文档放置](#文档放置)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 目录契约

```
ST/
├── Api/                 # .NET 后端（解决方案：Api/src/ST.slnx）
├── Web/                 # Vue 前端（pnpm）
├── docs/                # 人与 AI 文档（本规范中心）
├── .gitignore           # 根合并忽略规则
├── .gitattributes       # 换行与二进制
├── .cursor/             # Cursor 规则
├── .github/             # CI / Copilot 指令
└── .claude/             # Claude Code 项目说明
```

## 构建与依赖

**后端**：

```bash
dotnet build Api/src/ST.slnx
```

**前端**：

```bash
cd Web && pnpm install && pnpm build
```

CI 建议矩阵：**job 1** 构建解决方案；**job 2** 安装并构建 `Web`（可用缓存 pnpm store）。

## 文档放置

| 内容 | 位置 |
|------|------|
| AI 与编码规范真源 | `docs/ai/` |
| 架构鸟瞰 | `docs/architecture/` |
| 部署运行 | `docs/deploy/` |
| 数据存储索引 | `docs/database/` |
| 对外 API 集成入口 | `docs/api/` |
| 代码旁文档（已迁移） | `docs/ai/api/ServiceTemplate.md`、`docs/ai/api/Auth.md`、`docs/ai/common/Architecture.md` |

## 推荐方案

- 共享类型契约：优先 **OpenAPI** + 前端 hand-written types（或后续 openapi-typescript），避免手写漂移。
- 版本发布：可对 `Api` 与 `Web` 打 **同一 tag**，changelog 分区记录。

## 禁止事项

- 禁止重新引入 **Git Submodule** 管理 Api/Web。
- 禁止在根目录添加第二个并列 `.git`。
- 禁止把 `node_modules`、`.vs`、`bin/obj` 提交入库（根 `.gitignore` 已覆盖）。

## AI 注意事项

- 变更 API 时同步列出 **需更新的前端文件**（`Web/src/api`、`types`）。
- 引用路径时使用仓库相对路径：`Api/src/...`、`Web/src/...`。
