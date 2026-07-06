# cache skill

## 适用场景

Redis 缓存、Gateway 限流、权限缓存、租户配额缓存、库存 Lua。

## 必须先读

- `docs/database/README.md`
- `docs/backend/README.md`

## 常用源码路径

- `Api/src/Infrastructures/ST.Infra.Redis/`
- `Api/src/Microservices/Gateway/ST.Gateway/RateLimiting/`
- `Api/src/Microservices/Inventory/`

## 开发规则

- 键必须有命名空间。
- 缓存必须说明 TTL 和失效策略。
- 扣减/限流类场景必须原子化。
- 缓存失败不能破坏主业务一致性。

## 禁止事项

- 禁止非原子 GET 后 SET 做库存扣减。
- 禁止无限期缓存权限且不失效。

## 不确定时必须询问

- 键空间和 TTL 是什么？
- 是否允许缓存穿透到 DB？
- 是否需要租户隔离？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `git diff --check`
