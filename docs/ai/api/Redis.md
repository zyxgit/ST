# Redis 规范

## 目录

- [事实](#事实)
- [注册与注入](#注册与注入)
- [键与 TTL](#键与-ttl)
- [限流键空间](#限流键空间)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 事实

- 基础设施项目：`ST.Infra.Redis`，提供 `IRedisCacheManager`、`IRedisClient`、`IRateLimiter` 等（以当前注册为准）。
- `AddSharedWebApi` 会装配 Redis 相关能力（与配置中的 Redis 节联动）。

## 注册与注入

实际用法参考微服务中注入 `IRedisCacheManager` 或应用服务内封装（如 `TestService.RedisSet` 测试端点）。

## 键与 TTL

- 键前缀遵循 [`../common/Cache.md`](../common/Cache.md)。
- 会话类数据设置 **TTL**；永久键必须有淘汰策略说明。

## 权限缓存键空间

登录/刷新 Token 时缓存用户角色和权限到 Redis，减少 DB 四表联查压力。

**无租户**：
```
auth:user:{userId}:permissions    → HashSet (permissionCode → "1")
auth:user:{userId}:roles          → HashSet (roleCode → "1")
```

**有租户**（登录时指定了 `tenant_code`）：
```
t:{tenantId}:auth:user:{userId}:permissions    → HashSet (permissionCode → "1")
t:{tenantId}:auth:user:{userId}:roles          → HashSet (roleCode → "1")
```

| 键模式 | 类型 | TTL | 说明 |
|--------|------|-----|------|
| `auth:user:{userId}:permissions` | HashSet | Access Token 生命周期 | 用户权限码（无租户） |
| `auth:user:{userId}:roles` | HashSet | Access Token 生命周期 | 用户角色码（无租户） |
| `t:{tid}:auth:user:{userId}:permissions` | HashSet | Access Token 生命周期 | 用户权限码（租户隔离） |
| `t:{tid}:auth:user:{userId}:roles` | HashSet | Access Token 生命周期 | 用户角色码（租户隔离） |

**写入时机**：登录成功、刷新 Token 缓存未命中时。

**失效策略**：
- 用户角色变更（`UpdateUserAsync`）→ 删除该用户缓存键。
- 用户状态变更（`ChangeUserStatusAsync`）→ 删除该用户缓存键。
- 用户删除（`DeleteUserAsync`）→ 删除该用户缓存键。
- 角色/权限变更（`RoleService.UpdateAsync` / `ChangePermissionsAsync` / `DeleteAsync`）→ 全量清除 `auth:user:*` 模式键。
- TTL 兜底：Access Token 过期后自动清理。

**DI 注册**：无需额外注册，使用已有的 `IRedisCacheManager`。

## 登录限流键空间

多维度登录失败限流，防暴力破解。

```
auth:login:fail:ip:{ip}:email:{email}    → String (计数器)
auth:login:fail:ip:{ip}                  → String (计数器)
auth:login:fail:user:{userId}            → String (计数器)
```

| 键模式 | 类型 | TTL | 阈值 | 说明 |
|--------|------|-----|------|------|
| `auth:login:fail:ip:{ip}:email:{email}` | String | 10 分钟 | 10 次 | IP+邮箱 组合限流 |
| `auth:login:fail:ip:{ip}` | String | 10 分钟 | 50 次 | IP 总计限流 |
| `auth:login:fail:user:{userId}` | String | 30 分钟 | 5 次 | 用户维度限流（超限锁定账号） |

**行为**：
- 密码失败时同时递增三个计数器。
- IP+邮箱 或 IP 总计超限 → 返回"请求过于频繁"。
- 用户维度超限 → 账号锁定（`IsEnable=false`，`LockReason="login_fail_exceeded"`）。
- 登录成功 → 清除用户维度计数器。
- TTL 过期后自动重置计数。

## 库存预扣键空间

Inventory 服务使用 Redis Lua 脚本实现原子性库存预扣，作为 DB 乐观锁之前的热点防护层（双层防护）。

**无租户**：
```
inventory:sku:{skuId}:available   → String (可用库存计数器)
inventory:sku:{skuId}:frozen      → String (冻结库存计数器)
inventory:sku:{skuId}:sold        → String (已售库存计数器)
```

**有租户**（自动从 TenantContext 读取）：
```
t:{tenantId}:inventory:sku:{skuId}:available   → String
t:{tenantId}:inventory:sku:{skuId}:frozen      → String
t:{tenantId}:inventory:sku:{skuId}:sold        → String
```

| 键模式 | 类型 | TTL | 说明 |
|--------|------|-----|------|
| `inventory:sku:{skuId}:available` | String | 24h | SKU 可用库存（无租户） |
| `t:{tid}:inventory:sku:{skuId}:available` | String | 24h | SKU 可用库存（租户隔离） |

**工作流程**：

```
下单请求 → Redis Lua 预扣（available >= quantity → DECRBY available, INCRBY frozen）
                ↓ 成功                          ↓ 失败
         DB 乐观锁兜底                   直接返回库存不足（不打 DB）
                ↓ 成功
         冻结记录写入 DB
```

**Lua 脚本原子性**：`TryFreezeAsync`、`ReleaseAsync`、`ConfirmSoldAsync` 均使用 Lua 脚本保证原子操作。

**数据一致性**：
- SKU 创建/增加库存时，同步快照到 Redis（`SyncStockAsync`）。
- Redis 预扣成功但 DB 失败时，自动回滚 Redis。
- TTL 24h 兜底防泄漏，冷数据过期后从 DB 重新同步。

**DI 注册**：
```csharp
// 在 Program.cs 中
builder.Services.AddInventoryRedis();
```

## 限流键空间

分布式限流使用 Redis Sorted Set 实现滑动窗口，键格式：

```
rate:{ruleName}:{partitionKey}
```

| 键模式 | 说明 | 示例 |
|--------|------|------|
| `rate:auth-login:ip:{ip}` | 登录接口按 IP 限流 | `rate:auth-login:ip:192.168.1.1` |
| `rate:api-default:user:{userId}` | API 默认按用户限流 | `rate:api-default:user:uuid-123` |
| `rate:file-upload:ip:{ip}` | 文件上传按 IP 限流 | `rate:file-upload:ip:192.168.1.1` |

**TTL**：等于限流窗口大小（如 60 秒），过期后自动清理。

**DI 注册**：
```csharp
// 在 Program.cs 中
builder.Services.AddRedisInfra(builder.Configuration);
builder.Services.AddRedisRateLimiting();
```

## 代码示例

控制器层调用应用服务写入缓存（示意）：

```csharp
[HttpPost("redis")]
public async Task RedisSet(string key, string value)
{
	await _testService.RedisSet(key, value);
}
```

（出自 `ST.MS.Test.Api/Controllers/TestController.cs`。）

## 推荐方案

- 序列化使用 **UTF-8 文本**（JSON）并保持版本字段，便于演进。
- 热点 Key 使用 **滑动过期** 需评估 Redis CPU。

## 禁止事项

- 禁止把 Redis 当作 **唯一数据库** 持久化核心业务。
- 禁止无长度限制的 `LIST` 推送。

## AI 注意事项

- 生成缓存代码时同步说明 **失效点**（更新、删除、定时重建）。
