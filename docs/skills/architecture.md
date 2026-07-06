# architecture skill

## 适用场景

服务边界、Gateway、Aspire、Docker Compose、跨服务消息、可观测性、整体目录调整。

## 必须先读

- `docs/ai/README.md`
- `docs/architecture/README.md`
- `docs/devops/README.md`

## 常用源码路径

- `Api/src/Microservices/Gateway/ST.Gateway/appsettings.json`
- `Api/src/Aspire/ST.Aspire.AppHost/`
- `deploy/docker-compose.yml`
- `Api/src/Microservices/`
- `Api/src/Infrastructures/`

## 开发规则

- 新服务必须同步 Gateway、Aspire、Docker Compose、文档。
- 跨服务数据一致性使用事件、Outbox/Inbox、Saga。
- 服务不得直接访问其他服务数据库。

## 禁止事项

- 禁止只新增服务空壳不接运行入口。
- 禁止未经确认改变顶层目录结构。

## 不确定时必须询问

- 新能力属于哪个 bounded context？
- 是否必须通过 Gateway 暴露？
- 是否需要跨服务事务或消息？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `git diff --check`
