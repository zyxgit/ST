# cache.skill

## 1. Skill Name

`st-cache-redis` — Redis 使用方式与键空间纪律。

## 2. Purpose

- 约束服务端缓存抽象、键前缀、TTL；与多租户键格式对齐；降低缓存击穿导致的脏读。

## 3. Tech Stack

| 项 | 事实 |
|----|------|
| 库 | `ST.Infra.Redis`（`IRedisCacheManager`、`IRedisClient` 等按注册） |
| 注册 | 由 `AddSharedWebApi` 体系与配置节接入 |
| 典型用法 | 应用服务内封装读写；测试示例见 `TestController`/`TestService` Redis 演示 |

## 4. Architecture Rules

- 缓存**非**唯一数据源；写路径必须更新或删除缓存。
- 前后端键空间隔离：浏览器缓存 ≠ Redis。

## 5. Coding Rules

- 键：`st:<boundedContext>:<resource>:<id>`；多租户：`st:t:{tenantId}:...`（演进）。
- 必须 **TTL** 或事件失效；热点列表缓存慎用。

## 6. Naming Rules

- 锁前缀：`st:lock:<topic>:<id>`；短 TTL。

## 7. Best Practices

- JSON 序列化 UTF-8；版本字段支持无损演进。
- 读多写少：旁路缓存 + 过期；写多：先写 DB 再删缓存。

## 8. Forbidden Practices

- 永久键无淘汰策略堆积大集合。
- 缓存唯一权限副本且无 TTL。
- 密钥、refresh token 入 Redis 明文长期存储。

## 9. AI Generation Constraints

- 生成缓存读写必须同步写出 **失效条件**（更新/删除/TTL）。
- 不默认 `StackExchange.Redis` 直连除非已有模式；优先现有封装。

## 10. Example Code

```csharp
// ST.MS.Test.Application/Services/TestService.cs — IRedisCacheManager
public async Task RedisSet(string key, string value)
{
	await _redisManager.SetStringAsync(key, value, TimeSpan.FromMinutes(10));
}

public async Task<string?> RedisGet(string key)
{
	return await _redisManager.GetStringAsync(key);
}
```

```csharp
using ST.Infra.Redis.Cache;

public sealed class MyService(IRedisCacheManager redisManager)
{
	public Task SetDemo(string key, string value) =>
		redisManager.SetStringAsync(key, value, TimeSpan.FromMinutes(10));
}
```

## 11. Related Documents

- `docs/ai/api/Redis.md`
- `docs/ai/common/Cache.md`
- `docs/ai/skills/saas.skill.md`
