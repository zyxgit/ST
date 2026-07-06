# 多租户规范

## 目录

- [概述](#概述)
- [数据隔离策略](#数据隔离策略)
- [租户上下文](#租户上下文)
- [JWT 与登录](#jwt-与登录)
- [实体租户化](#实体租户化)
- [EF Core 全局过滤器](#ef-core-全局过滤器)
- [Redis 键空间](#redis-键空间)
- [限流租户维度](#限流租户维度)
- [租户配额](#租户配额)
- [IntegrationEvent 传播](#integrationevent-传播)
- [OperationLog 租户化](#operationlog-租户化)
- [API 端点](#api-端点)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 概述

ST 项目支持多租户 SaaS 部署，采用**共享库 + TenantId** 数据隔离策略。所有业务实体通过实现 `ITenantEntity` 接口自动参与租户隔离，EF Core 全局查询过滤器保证数据不可见性。

## 数据隔离策略

| 模式 | 适用 | ST 选择 |
|------|------|---------|
| 共享库 + TenantId | 中小 SaaS、成本低 | ✅ 默认方案 |
| 每租户独立 Schema | 合规要求高 | 未实现 |
| 每租户独立库 | 超大客户 | 未实现 |

**核心机制**：
- 所有业务表增加 `tenant_id` 字段（GUID）
- EF Core `HasQueryFilter` 自动附加 `WHERE tenant_id = @currentTenantId`
- 新增实体时 `SaveChanges` 自动填充 `TenantId`
- PostgreSQL RLS 可作为后续增强

## 租户上下文

### ICurrentTenantAccessor

```csharp
// ST.Shared/Security/ICurrentTenantAccessor.cs
public interface ICurrentTenantAccessor
{
    Guid? TenantId { get; }
    string? TenantCode { get; }
}
```

### TenantContext（AsyncLocal）

```csharp
// ST.Shared/TenantContext.cs
public static class TenantContext
{
    public static Guid? CurrentTenantId { get; set; }  // AsyncLocal
    public static IDisposable BeginScope(Guid? tenantId);
}
```

### 数据流

```
HTTP 请求 (JWT tid)
  → HttpUserContext.TenantId (读取 claim)
  → HttpCurrentTenantAccessor (同步到 TenantContext)
  → TenantContext.CurrentTenantId (AsyncLocal)
  → EF Core 查询过滤器 / SaveChanges 自动填充
  → IntegrationEvent.TenantId (构造函数自动提取)
  → RabbitMQ x-tenant-id header
  → 消费端 TenantContext 恢复
```

## JWT 与登录

### JWT Claims

| Claim | Key | 说明 |
|-------|-----|------|
| 租户 ID | `tid` | GUID 格式 |
| 租户编码 | `tcode` | 小写字母+数字 |

### 登录请求

```json
{
  "email": "user@example.com",
  "password": "****",
  "tenant_code": "acme"
}
```

- `tenant_code` 为可选字段，不填则不绑定租户
- 验证用户属于该租户（`tenant_users` 表）
- 验证租户状态为 `Active`
- JWT 写入 `tid` 和 `tcode`

### RefreshToken

RefreshToken 实体存储 `TenantId` 和 `TenantCode`，刷新时自动恢复租户上下文。租户失效后自动降级为无租户模式。

## 实体租户化

### ITenantEntity 接口

```csharp
// ST.Infra.Repository/Entities/ITenantEntity.cs
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

### 已租户化的实体

| 实体 | 项目 | 说明 |
|------|------|------|
| `Order` | Order.Domain | 订单 |
| `Sku` | Inventory.Domain | SKU 库存 |
| `Payment` | Payment.Domain | 支付记录 |
| `FileEntity` | FileUpload.Domain | 文件记录 |
| `OperationLog` | Infra.EntityFramework | 操作日志 |

### TenantDomainEntity 基类

```csharp
// ST.Shared.Domain/Entites/TenantDomainEntity.cs
public abstract class TenantDomainEntity : DomainEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
}
```

新实体可选择继承此基类，或直接实现 `ITenantEntity` 接口。

## EF Core 全局过滤器

`EfDbContextBase.OnModelCreating` 自动为所有 `ITenantEntity` 实体应用查询过滤器：

```sql
WHERE (is_deleted = false OR is_deleted IS NULL)
  AND (tenant_id = @currentTenantId OR @currentTenantId IS NULL)
```

- `TenantContext.CurrentTenantId == null` → 不过滤（超级管理员/后台任务）
- `TenantContext.CurrentTenantId == some-guid` → 只返回该租户数据
- ISoftDelete + ITenantEntity → 自动合并为 AND

### 自动填充 TenantId

`NpgsqlEfDbContextBase.FillAuditFields()` 在 `SaveChanges` 时自动为 `Added` 状态的 `ITenantEntity` 实体填充 `TenantId`（从 `TenantContext.CurrentTenantId`）。

## Redis 键空间

业务数据键统一加入租户前缀 `t:{tenantId}:`：

| 键模式 | 无租户 | 有租户 (tid=abc123) |
|--------|--------|---------------------|
| 权限缓存 | `auth:user:{userId}:permissions` | `t:abc123:auth:user:{userId}:permissions` |
| 角色缓存 | `auth:user:{userId}:roles` | `t:abc123:auth:user:{userId}:roles` |
| 库存 available | `inventory:sku:{skuId}:available` | `t:abc123:inventory:sku:{skuId}:available` |
| 库存 frozen | `inventory:sku:{skuId}:frozen` | `t:abc123:inventory:sku:{skuId}:frozen` |
| 库存 sold | `inventory:sku:{skuId}:sold` | `t:abc123:inventory:sku:{skuId}:sold` |
| 配额缓存 | — | `t:tenant:quota:abc123:max_orders` |

**不租户化的键**（安全防护，跨租户共享）：

| 键模式 | 说明 |
|--------|------|
| `auth:login:fail:ip:{ip}:email:{email}` | IP+邮箱登录限流 |
| `auth:login:fail:ip:{ip}` | IP 总计登录限流 |
| `auth:login:fail:user:{userId}` | 用户登录限流 |
| `rate:{ruleName}:{partitionKey}` | Gateway 限流（除非使用 Tenant 维度） |

## 限流租户维度

Gateway 限流支持三种租户分区维度：

| 维度 | 键格式 | 适用场景 |
|------|--------|----------|
| `Tenant` | `tenant:{tid}` | 租户级全局限流 |
| `TenantUser` | `tenant:{tid}:user:{uid}` | 租户内用户级限流 |
| `TenantPath` | `tenant:{tid}:path:{path}` | 租户特定接口限流 |

配置示例见 [`Gateway.md`](../api/Gateway.md#租户级限流配置)。

## 租户配额

### 配额模型

```sql
tenant_quotas
- tenant_id          UUID UNIQUE
- max_users          INT (默认 100)
- max_storage_bytes  BIGINT (默认 10GB)
- max_api_calls_per_day INT (默认 100000)
- max_file_size      BIGINT (默认 100MB)
- max_orders_per_day INT (默认 10000)
```

### 配额检查

| 检查点 | 服务 | 方法 |
|--------|------|------|
| 每日订单数 | OrderService | `ITenantQuotaService.CheckOrderQuotaAsync` |
| 单文件大小 | FileAppService | `ITenantQuotaService.CheckFileSizeQuotaAsync` |
| 存储容量 | FileAppService | `ITenantQuotaService.CheckStorageQuotaAsync` |

配额限制从 IdentityDbContext 查询，Redis 缓存 1 小时。超限返回 `BusinessException`。

## IntegrationEvent 传播

`IntegrationEvent` 基类新增 `TenantId` 属性，构造函数自动从 `TenantContext.CurrentTenantId` 提取。

RabbitMQ 发布时写入 `x-tenant-id` header，消费时自动恢复 `TenantContext`。

## OperationLog 租户化

- `OperationLog` 实体新增 `TenantId` 字段
- `OperationLogActionFilter` 从 `IUserContext.TenantId` 填充
- 批量消费者映射到数据库实体
- 操作日志查询可按租户过滤

## API 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/api/tenants` | 分页查询租户 |
| GET | `/api/tenants/{id}` | 租户详情 |
| POST | `/api/tenants` | 创建租户 |
| PUT | `/api/tenants/{id}` | 更新租户 |
| POST | `/api/tenants/{id}/activate` | 激活租户 |
| POST | `/api/tenants/{id}/suspend` | 暂停租户 |
| DELETE | `/api/tenants/{id}` | 删除租户 |
| POST | `/api/tenants/{tenantId}/users` | 添加租户用户 |
| DELETE | `/api/tenants/{tenantId}/users/{userId}` | 移除租户用户 |
| GET | `/api/tenants/{tenantId}/users` | 查询租户用户 |
| GET | `/api/tenants/{tenantId}/quota` | 查询租户配额 |
| PUT | `/api/tenants/{tenantId}/quota` | 更新租户配额 |

## 禁止事项

- 禁止客户端任意指定租户 ID 且无服务端校验（越权风险）
- 禁止在无租户隔离审计前把演示库数据直接用于多租户生产
- 禁止在未设置 `TenantContext.CurrentTenantId` 的情况下查询租户数据（后台任务需显式设置）

## AI 注意事项

- AI 生成 CRUD 时，若需求含"租户"，必须在**实体 + DTO + 查询**三层同时体现 `TenantId`
- 新增实体若需租户隔离，实现 `ITenantEntity` 接口即可自动获得过滤和填充
- 不在未确认模型前自动生成 RLS SQL——需 DBA 评审
- 后台服务（无 HTTP 上下文）需通过 `TenantContext.BeginScope(tenantId)` 或从消息 header 恢复租户上下文
