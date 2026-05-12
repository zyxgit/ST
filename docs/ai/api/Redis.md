# Redis 规范

## 目录

- [事实](#事实)
- [注册与注入](#注册与注入)
- [键与 TTL](#键与-ttl)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 事实

- 基础设施项目：`ST.Infra.Redis`，提供 `IRedisCacheManager`、`IRedisClient` 等（以当前注册为准）。
- `AddSharedWebApi` 会装配 Redis 相关能力（与配置中的 Redis 节联动）。

## 注册与注入

实际用法参考微服务中注入 `IRedisCacheManager` 或应用服务内封装（如 `TestService.RedisSet` 测试端点）。

## 键与 TTL

- 键前缀遵循 [`../common/Cache.md`](../common/Cache.md)。
- 会话类数据设置 **TTL**；永久键必须有淘汰策略说明。

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
