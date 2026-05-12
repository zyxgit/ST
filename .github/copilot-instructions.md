# GitHub Copilot — ST Monorepo 指令

本指令与 `docs/ai/`、`.claude/CLAUDE.md`、`.cursor/rules/` **保持一致**。

## 项目结构

- `Api/src/ST.slnx`：.NET 解决方案；微服务在 `Api/src/Microservices/`；共享库在 `Api/src/ServiceShared/` 与 `Api/src/Infrastructures/`。
- `Web/src`：Vue 3 + TypeScript 管理端；`lib/request.ts` 为唯一 Axios 入口；`stores/auth.ts` 负责登录态与菜单 bootstrap。

## 代码生成原则

- **后端**：Controller 继承 `AbstractControllerBase`；应用服务在 `*.Application`；EF 在 `*.Infra` 用 `AddNpgsqlDbContextFromConfig<TContext>()`；异常用 `BusinessException`/`DomainException`。
- **前端**：新页面加入 `router/routes.ts`；需权限则设置 `meta.permission: PermissionCode.xxx`；API 放在 `src/api/*.ts` 并用 `request` 调用。
- **安全**：不输出或硬编码 JWT 密钥、连接串、SMTP 密码；使用环境变量与 UserSecrets。
- **文档（强制）**：功能或对外契约有变化时，在同一变更中按 `docs/ai/common/DocumentationSync.md` **更新或新增** 相关 Markdown（含 `docs/ai/**` 与架构/部署等索引）；禁止提交空文档或与代码不一致的路径/配置键说明。

## 错误与 API 形态

- 后端统一 ProblemDetails，扩展 `traceId`、可选 `errorCode`。
- 前端 Axios 拦截器已用 `message`/`detail`/`title` 展示错误，避免重复弹窗。

## 多租户与 SaaS

- 详见 `docs/ai/common/MultiTenant.md`；未落地前仅作预留，不擅自加全局 Tenant 过滤器而不评审。

## 应优先引用的文档路径

- 总览：`docs/ai/README.md`
- Agent 高密度索引：`docs/ai/skills/README.md` 与各 `*.skill.md`
- 后端清单：`docs/ai/api/AI-Rules.md`
- 前端清单：`docs/ai/web/AI-Rules.md`

生成代码后，用 `dotnet build Api/src/ST.slnx` 与 `cd Web && pnpm build` 作为基本验证（若环境可用）。
