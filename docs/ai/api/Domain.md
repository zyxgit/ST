# Domain 层规范

## 目录

- [职责](#职责)
- [实体基类](#实体基类)
- [异常语义](#异常语义)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 职责

- 封装领域实体、值对象、领域服务接口（若采用）。
- 依赖 **`ST.Shared.Domain`** 提供的聚合根基元（如 `Entity`、`AggregateRoot` 等，以仓库代码为准）。

## 实体基类

仓库中存在审计、软删等接口（如 `IBasicAuditInfo`、`ISoftDelete`，路径：`ST.Infra.Repository/Entities`）。新实体按现有微服务中实体写法继承或实现对应接口，保持 **迁移与 EF 配置一致**。

## 异常语义

- **`DomainException`**：领域规则不满足（例如不变式被破坏），由 `GlobalExceptionMiddleware` 映射为 HTTP 400，`Title` 为「验证失败」。
- **`BusinessException`**：业务可预期错误（库存不足、重复订阅等），带 **HTTP 状态码** 与可选 **`errorCode`**。

```csharp
throw new DomainException("邮箱格式无效");
throw new BusinessException("余额不足", statusCode: 402, errorCode: "PAY_INSUFFICIENT");
```

## 代码示例

领域模块注册示例（节选概念）：

```csharp
public sealed class DomainModule : ServiceModule
{
	public override void ConfigureServices(IServiceCollection services)
	{
		base.ConfigureServices(services);
		// 领域服务接口→实现注册
	}
}
```

## 推荐方案

- 领域逻辑 **纯 .NET**，不引用 ASP.NET Core / EF Core 类型。
- 与持久化映射无关的计算留在实体方法或领域服务。

## 禁止事项

- 禁止 `Domain` 项目引用 `*.Infra`（依赖倒置：Infra 引用 Domain）。
- 禁止在实体上附带 UI 或配置专属特性。

## AI 注意事项

- 新增实体时同步更新 **`*.Infra` 中的 EF Configuration / Fluent API**，并生成迁移。
