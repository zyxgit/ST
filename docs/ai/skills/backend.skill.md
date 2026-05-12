# backend.skill

## 1. Skill Name

`st-backend-dotnet` — ST Monorepo 后端（`Api/src`）开发与约束。

## 2. Purpose

- 约束分层、启动管道、异常、分页、EF、任务调度，降低 AI 生成与仓库不一致的 C# 代码。
- 索引关键类型与路径，供 Agent 检索。

## 3. Tech Stack

| 层 | 事实 |
|----|------|
| Runtime | .NET（解决方案 `Api/src/ST.slnx`） |
| Web | ASP.NET Core，`AddSharedWebApi` / `UseSharedWebApi` |
| ORM | EF Core + Npgsql（`AddNpgsqlDbContextFromConfig<TContext>()`） |
| DB | PostgreSQL |
| Cache | Redis（`ST.Infra.Redis`） |
| Jobs | `IBackgroundTaskScheduler` → Hangfire 实现 |
| Gateway | 独立项目 `ST.Gateway`，YARP（非各微服务内嵌） |

## 4. Architecture Rules

- 微服务目录：`Api/src/Microservices/<Name>/ST.MS.<Name>.{Api|Application|Domain|Infra}`。
- 依赖方向：`*.Api` → `*.Application` → `*.Domain`；`*.Infra` 实现持久化；共享：`Api/src/ServiceShared`、`Api/src/Infrastructures`。
- 启动：`Program.cs` 使用 `builder.AddServiceDefaults()`、`builder.AddSharedWebApi(modules)`、`app.UseSharedWebApi(modules)`；模块实现 `ISharedModule`（常见 `ServiceModule`）。
- HTTP：控制器继承 `ST.Shared.WebApi.Controller.AbstractControllerBase`（默认 `[Authorize]`、`[Route("api/[controller]")]`）。

## 5. Coding Rules

- 业务失败：`throw new BusinessException(message, statusCode, errorCode)`；领域规则：`throw new DomainException(message)`（中间件分别映射 ProblemDetails）。
- 分页入参：`PagedRequestDto`，调用 `Normalize()`；出参：`PagedResultDto<T>`（`ST.Shared.Application.Dtos`）。
- EF：`InfraModule` 内 `services.AddNpgsqlDbContextFromConfig<YourDbContext>()`；迁移在 `*.Infra` 生成。
- 后台任务：注入 `IBackgroundTaskScheduler`（`Enqueue` / `Schedule` / `Recurring` / `Remove`）。

## 6. Naming Rules

- 程序集：`ST.MS.<Service>.<Layer>`。
- 应用服务：`*Service`；模块：`*Module`；DbContext：语义化 + `DbContext` 后缀。
- 异步方法：后缀 `Async`。

## 7. Best Practices

- 新功能落在单一 bounded context 的微服务项目内。
- 配置：`Database__*`、`Jwt__*` 等环境变量覆盖；不写死密钥。
- OpenAPI：Development 下各服务 `MapOpenApi` / Scalar（按现有 `Program.cs` 模式）。

## 8. Forbidden Practices

- 在 `Domain` 项目引用 `*.Infra` 或 `Microsoft.EntityFrameworkCore`（除非团队明确打破洋葱）。
- 裸 `throw new Exception(...)` 表达可预期业务错误。
- 绕过 `UseSharedWebApi` 管道复制一套中间件。
- 列表返回随意匿名对象而不使用 `PagedResultDto<T>`（分页场景）。

## 9. AI Generation Constraints

- 新建 Controller 前 `glob` 是否已有同名路由；必须标注 `[Authorize]` / `[AllowAnonymous]` 意图。
- 生成 EF 迁移命令须含 `--project`（Infra）与 `--startup-project`（Api）。
- 禁止臆造不存在的基类；共享类型仅从 `ST.Shared.*`、`ST.Infra.*` 引用。
- 变更须同步 `docs/ai/common/DocumentationSync.md` 所列 md。

## 10. Example Code

```csharp
// ST.Shared.WebApi.Controller — 默认需登录；分页 DTO 来自 ST.Shared.Application.Dtos
public class ReportsController : AbstractControllerBase
{
	[HttpGet("list")]
	public ActionResult<PagedResultDto<string>> List([FromQuery] PagedRequestDto request)
	{
		var (pageIndex, pageSize, skip) = request.Normalize();
		return Ok(new PagedResultDto<string>
		{
			PageIndex = pageIndex,
			PageSize = pageSize,
			TotalCount = 0,
			Items = []
		});
	}
}
```

```csharp
// InfraModule — 注册 DbContext
services.AddNpgsqlDbContextFromConfig<AppDbContext>();
```

## 11. Related Documents

- `docs/ai/api/README.md`
- `docs/ai/api/Application.md`、`Domain.md`、`EFCore.md`、`Exception.md`、`Result.md`、`DTO.md`、`Hangfire.md`
- `docs/ai/api/ServiceTemplate.md`、`docs/ai/common/Architecture.md`
