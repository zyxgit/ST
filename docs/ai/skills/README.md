# ST AI Skill Center

高密度 Agent 技能索引；真源见 `docs/ai/common|api|web`。

| Skill | 文件 | 覆盖 |
|-------|------|------|
| Backend | [backend.skill.md](./backend.skill.md) | .NET、分层、EF、异常、分页 |
| Frontend | [frontend.skill.md](./frontend.skill.md) | Vue3、Pinia、路由、Axios |
| Architecture | [architecture.skill.md](./architecture.skill.md) | Monorepo、微服务、网关、Aspire |
| Database | [database.skill.md](./database.skill.md) | PostgreSQL、EF、迁移 |
| SaaS | [saas.skill.md](./saas.skill.md) | 多租户预留、演进边界 |
| Upload | [upload.skill.md](./upload.skill.md) | 上传约束与演进 |
| Auth | [auth.skill.md](./auth.skill.md) | JWT、Policy、IUserContext |
| Cache | [cache.skill.md](./cache.skill.md) | Redis、键、TTL |
| Logging | [logging.skill.md](./logging.skill.md) | NLog、中间件、级别 |
| Prompt | [prompt.skill.md](./prompt.skill.md) | 提示词与文档同步 |

阅读顺序：`architecture` → `backend`/`frontend` → 横切 `auth`/`cache`/`logging`。
