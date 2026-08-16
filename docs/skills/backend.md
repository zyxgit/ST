# backend skill

## 适用场景

.NET 微服务、Controller、Application、Domain、Infra、EF、消息、后台任务。

## 必须先读

- `docs/ai/README.md`
- `docs/backend/README.md`
- `docs/backend/service-template.md`
- `docs/backend/api-routing.md`
- `docs/database/README.md`

## 常用源码路径

- `Api/src/Microservices/<Service>/`
- `Api/src/ServiceShared/`
- `Api/src/Infrastructures/`

## Application 层目录规范

Application 层内部按职责分文件夹，禁止接口和实现混放：

```text
ST.MS.<Service>.Application/
├── IServices/          # 业务接口（I{X}Service），继承 IAppService
├── Services/           # 业务实现（{X}Service）、集成事件 Handler、后台任务
├── Options/            # 配置 POCO 类（{X}Options）
└── Dto/                # 数据传输对象
```

- **接口**放 `IServices/`，命名空间 `ST.MS.<Service>.Application.IServices`，继承 `IAppService`。
- **实现类**放 `Services/`，继承 `AbstractAppService, I{X}Service`（不再单独写 `ITransientDependency`，`AbstractAppService` 已实现）。
- **配置类**放 `Options/`，命名空间 `ST.MS.<Service>.Application.Options`，不与业务 Service 混放。
- **Handler / 后台任务**放 `Services/`，它们是实现类，不是接口。
- DI 注册由 Autofac 程序集扫描自动完成（`ITransientDependency` / `IScopedDependency` / `ISingletonDependency`），无需手动注册。

## 开发规则

- 新服务先按 service-template 检查 Gateway/Aspire/Docker Compose，避免 502。
- 新接口先按 api-routing 写清外部路径、下游路径、Controller Route，避免 404。
- Controller 薄层，业务进入 Application/Domain。
- DTO 不直接暴露 EF 实体。
- 新实体必须有迁移。
- 跨服务事件使用 IntegrationEvents + Outbox/Inbox。

## 禁止事项

- 禁止业务逻辑堆在 Controller。
- 禁止直接写其他服务数据库。
- 禁止无迁移修改实体。
- 禁止只验证下游路径不验证 Gateway 路径。

## 不确定时必须询问

- 接口是否需要鉴权和权限码？
- 是否需要事务、幂等或补偿？
- 是否影响 Gateway 或前端？
- 外部路径和 Transform 后下游路径分别是什么？
- 服务实际监听端口和 Gateway destination 是否一致？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `git diff --check`
