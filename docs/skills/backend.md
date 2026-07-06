# backend skill

## 适用场景

.NET 微服务、Controller、Application、Domain、Infra、EF、消息、后台任务。

## 必须先读

- `docs/ai/README.md`
- `docs/backend/README.md`
- `docs/database/README.md`

## 常用源码路径

- `Api/src/Microservices/<Service>/`
- `Api/src/ServiceShared/`
- `Api/src/Infrastructures/`

## 开发规则

- Controller 薄层，业务进入 Application/Domain。
- DTO 不直接暴露 EF 实体。
- 新实体必须有迁移。
- 跨服务事件使用 IntegrationEvents + Outbox/Inbox。

## 禁止事项

- 禁止业务逻辑堆在 Controller。
- 禁止直接写其他服务数据库。
- 禁止无迁移修改实体。

## 不确定时必须询问

- 接口是否需要鉴权和权限码？
- 是否需要事务、幂等或补偿？
- 是否影响 Gateway 或前端？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `git diff --check`
