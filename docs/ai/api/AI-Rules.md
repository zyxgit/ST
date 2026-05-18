# 后端 AI 生成规则（强制对齐）

## 目录

- [真源](#真源)
- [必须遵守](#必须遵守)
- [生成前检查清单](#生成前检查清单)
- [禁止](#禁止)

## 真源

- 架构：`docs/ai/common/Architecture.md`、`docs/ai/api/*.md`
- 模板：`docs/ai/api/ServiceTemplate.md`

## 必须遵守

1. **分层**：新代码落在正确的 `*.Api` / `*.Application` / `*.Domain` / `*.Infra` 项目。
2. **启动**：`Program.cs` 使用 `AddServiceDefaults` + `AddSharedWebApi(modules)` + `UseSharedWebApi(modules)`。
3. **异常**：业务 `BusinessException`，领域规则 `DomainException`，不得滥用裸 `Exception`。
4. **返回**：列表分页使用 **`PagedRequestDto` / `PagedResultDto<T>`**。
5. **持久化**：EF 变更必须包含 **迁移命令说明** 与 **DbContext** 名称。
6. **安全**：默认 `[Authorize]`；公开接口显式 `[AllowAnonymous]`。
7. **操作日志**：写操作（增/删/改）必须标记 `[OperationLog]`；读操作按需标记；下载端点 `RecordResponse = false`。规则优先级：以 `docs/ai/api/【对应域】.md` 中的操作日志表为准。
8. **文档**：功能或契约变化时，按 [`../common/DocumentationSync.md`](../common/DocumentationSync.md) **补充或新增** `docs/ai/**`（及 `docs/architecture|deploy|database|api` 索引）中的相关 Markdown，**禁止留空文档**。

## 生成前检查清单

- [ ] 是否已有同名 `Controller`/`DbSet`？
- [ ] 路由是否与 **网关** `ReverseProxy` 前缀冲突？
- [ ] JWT 权限策略是否 **`perm:`** 前缀？
- [ ] Redis 键是否带业务前缀？
- [ ] 是否已列出并更新将受影响的 **`docs/ai/**/*.md`**（及架构/部署索引）？

## 禁止

- 禁止发明不存在的共享基类（必须先 `grep`/`glob`）。
- 禁止写死生产 URL、密钥、连接串。
- 禁止一次 PR 无关重构整个解决方案。
- 禁止**仅改后端代码**而不更新对外契约或 AI 规范文档（当该功能对集成方或 Agent 可见时）。
