# saas.skill

## 1. Skill Name

`st-saas-multitenant-evolution` — SaaS 方向、多租户预留与微服务演进边界。

## 2. Purpose

- 约束租户标识、数据隔离策略的表述方式；禁止 AI 在未评审情况下写入全局租户过滤器或客户端可控租户头。

## 3. Tech Stack

| 项 | 状态 |
|----|------|
| Monorepo | 已采用（`docs/ai/common/Monorepo.md`） |
| 微服务形态 | `Api/src/Microservices/*` 已存在 |
| 多租户数据模型 | **预留**，以产品为准 |
| JWT | 当前用户/权限claims；未来可加 `tid` 等 |

## 4. Architecture Rules

- **演进**：服务边界按域拆分；数据库-per-service 倾向，避免共享库跨服务写。
- **租户**：默认演进路径为共享库 + **TenantId** 列 + 复合唯一索引；RLS/独立 schema 需单独设计文档。
- **网关**：租户级路由或校验可在 `ST.Gateway` 层扩展，与下游服务约定 header/claim。

## 5. Coding Rules

- 若引入 `TenantId`：实体 + DTO + 查询 + 缓存键 **同时**带租户段（见 `docs/ai/common/MultiTenant.md`）。
- 权限仍以 JWT + `perm:` / 前端 `PermissionCode` 为主；租户与权限正交。

## 6. Naming Rules

- 租户标识：`tid`（claim）或 `X-Tenant-Id`（仅内网可信）；缓存键 `st:t:{tenantId}:...`。

## 7. Best Practices

- 功能开关与套餐：配置中心或 DB 表驱动，避免硬编码租户名单。
- 审计字段与操作日志已有基础设施时优先复用。

## 8. Forbidden Practices

- 客户端任意传 **租户 ID** 且无服务端校验。
- 在未立项前全局 `HasQueryFilter` 加租户忽略性能影响。
- 将演示库数据直接当多租户生产模板。

## 9. AI Generation Constraints

- 不自动生成完整租户 CRUD 除非需求明确要求；若生成须列 **越权风险** 与测试点。
- 文档变更同步 `docs/ai/common/MultiTenant.md`、`saas.skill` 引用链。

## 10. Example Code

```csharp
// 缓存键含租户（演进示例，键格式见 Cache.skill）
// st:t:{tenantId}:user:{userId}
```

```json
// JWT 演进预留（非当前强制）
{ "tid": "018f9e3b-7c2a-7e5f-a7b2-7f8e9d0c1a2b" }
```

## 11. Related Documents

- `docs/ai/common/MultiTenant.md`、`Cache.md`、`Prompt.md`
- `docs/ai/common/DocumentationSync.md`
- `docs/architecture/README.md`
