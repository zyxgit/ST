# 缓存规范（通用）

## 目录

- [原则](#原则)
- [键命名](#键命名)
- [与后端 Redis 的关系](#与后端-redis-的关系)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 原则

- 缓存用于 **降低延迟与数据库压力**，不能作为 **唯一真相源**；写路径必须更新或失效缓存。
- **Monorepo 级**：前后端不得假设同一键空间——浏览器缓存 / Service Worker 与 Redis **隔离**。

## 键命名

推荐命名空间前缀（Redis）：

```
st:<boundedContext>:<resource>:<id>
```

示例：

```
st:identity:user:12345
st:test:item:abc-def
```

## 与后端 Redis 的关系

- 服务端缓存实现见 `ST.Infra.Redis` 与 [`../api/Redis.md`](../api/Redis.md)。
- 租户维度预留：键中预留 `t:{tenantId}` 段（见 [`MultiTenant.md`](./MultiTenant.md)）。

## 推荐方案

- 热点列表：短 TTL + 分页缓存谨慎使用（易不一致）。
- 分布式锁：与业务锁区分前缀 `st:lock:<topic>:<id>`，设置 TTL 防止死锁。

## 禁止事项

- 禁止缓存 **用户权限唯一副本**（权限变更必须失效或短 TTL）。
- 禁止无 TTL 的大集合缓存键无限增长。

## AI 注意事项

- 生成读写缓存代码时，必须给出 **失效策略**（事件触发 delete / 版本号 / TTL）。
- 不在前端长期缓存敏感个人信息于 `localStorage`（令牌存储策略见 `Web` 现有 `auth/token` 实现）。
