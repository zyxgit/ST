# Application 层规范

## 目录

- [职责](#职责)
- [模块](#模块)
- [应用服务](#应用服务)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 职责

- 编排用例、事务边界（与 `IUnitOfWork` / 拦截器协同）、调用领域与基础设施。
- DTO 入参/出参定义于 Application 项目（如 `ST.MS.*.Application/Dto`）。

## 模块

每个微服务通常包含：

- `ApplicationModule : ServiceModule`，在 `ConfigureServices` 中注册本层服务。
- 与 `Program.cs` 中 `ISharedModule[]` 一并传入 `AddSharedWebApi(modules)`。

## 应用服务

- 可继承 `AbstractAppService`（`ST.Shared.Application.Services`），并实现 `ITransientDependency`（若需要依赖标记扫描）。
- **不写** HTTP 细节（避免引用 `HttpContext`，除非通过 `ICurrentUserIdAccessor` / `IUserContext` 抽象）。

## 代码示例

应用服务构造注入仓储与其它应用服务：

```csharp
namespace ST.MS.Test.Application.Services;

public class TestService
{
	private readonly ILogger<TestService> _logger;

	public TestService(ILogger<TestService> logger)
	{
		_logger = logger;
	}

	public string Test() => "ok";
}
```

分页请求使用共享类型：

```csharp
using ST.Shared.Application.Dtos;

var request = new PagedRequestDto { PageIndex = 1, PageSize = 20 };
var (pageIndex, pageSize, skip) = request.Normalize();
```

## 推荐方案

- 同一聚合的变更放在一个应用服务方法内，确保 UnitOfWork 一致。
- 跨服务调用使用 **HTTP/Rabbit**（按演进选型），不在 Application 层直连其它服务的 DbContext。

## 禁止事项

- 禁止在 Application 层直接使用 **`DbSet<T>`**（应通过仓储或领域接口）。
- 禁止绕过共享异常类型抛出裸 `Exception` 表达可预期业务失败（应使用 `BusinessException` / `DomainException`）。

## AI 注意事项

- 新建 `*Service` 时核对命名空间是否与 **`ST.MS.<Service>.Application`** 一致。
- 分页返回值使用 **`PagedResultDto<T>`**（见 `Result.md`）。
