# Repository 规范

## 目录

- [接口位置](#接口位置)
- [约定](#约定)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 接口位置

- 泛型标记：`ST.Infra.Repository.Interface.IRepository<TEntity>`（可作为仓储抽象标记扩展）。
- 具体仓储接口与实现放在 **对应微服务的 `*.Infra`** 项目中。

## 约定

- **读路径**：通过仓储或 `DbContext` 查询（按现有服务风格）。
- **写路径**：配合 **`IUnitOfWork`** / `UnitOfWorkAttribute` / 拦截器统一提交（见 EF Core 与现有 Controller/AppService 用法）。

## 代码示例

接口定义（节选真实仓库）：

```csharp
namespace ST.Infra.Repository.Interface;

public interface IRepository<TEntity> where TEntity : class
{ }
```

在 `Infra` 中实现具体 `IUserRepository` 等，并在 `InfraModule.ConfigureServices` 里 `services.AddScoped<...>()`。

## 推荐方案

- 对复杂查询使用 **规约模式** 或 **专用查询类**，避免在 Application 中堆 `IQueryable` 组合。
- 跨聚合加载使用显式 **Include** 或拆分查询，注意 N+1。

## 禁止事项

- 禁止在 `Web` 层或 `Api` 层绕开服务直接 new `DbContext`（须用 DI）。
- 禁止在仓储中写与 **HTTP 状态** 强耦合的逻辑（应抛领域/业务异常）。

## AI 注意事项

- 若用户要求“通用 Crud 基类”，先检查 **是否已有** 基类或 `Repository` 实现，再扩展，避免重复第二套抽象。
