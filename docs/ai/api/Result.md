# 返回与分页规范

## 目录

- [成功响应](#成功响应)
- [分页类型](#分页类型)
- [UTC 时间](#utc-时间)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 成功响应

- 控制器返回 **`IActionResult` / `ActionResult<T>`**，常规 JSON 使用 **`Ok(dto)`**。
- 继承 `AbstractControllerBase` 时已 `[ApiController]`，序列化遵循 ASP.NET Core 默认。

## 分页类型

共享 DTO（`ST.Shared.Application.Dtos`）：

```csharp
public sealed class PagedResultDto<T>
{
	public required int PageIndex { get; init; }
	public required int PageSize { get; init; }
	public required long TotalCount { get; init; }
	public IReadOnlyList<T> Items { get; init; } = [];
}
```

请求侧 **`PagedRequestDto`**：

```csharp
public class PagedRequestDto
{
	public int PageIndex { get; set; } = 1;
	public int PageSize { get; set; } = 20;

	public (int PageIndex, int PageSize, int Skip) Normalize()
	{
		var pageIndex = PageIndex <= 0 ? 1 : PageIndex;
		var pageSize = PageSize <= 0 ? 20 : Math.Min(PageSize, 100);
		var skip = (pageIndex - 1) * pageSize;
		return (pageIndex, pageSize, skip);
	}
}
```

## UTC 时间

- **推荐**：数据库与时区无关字段使用 **`timestamp with time zone`**（Npgsql）或 **UTC `DateTime`**（`DateTimeKind.Utc`）。
- API 输出 JSON 使用 **ISO 8601** 带 `Z` 或显式 offset；前端 `dayjs` 统一解析（见前端文档）。

## 推荐方案

- 列表接口统一 `(items, totalCount, pageIndex, pageSize)` → **`PagedResultDto<T>`**。
- 避免 `dynamic` 或匿名类型跨层传递。

## 禁止事项

- 禁止返回 **`IQueryable`** 到控制器外。
- 禁止把 **敏感字段**（哈希盐、内部 Id）暴露给非管理员 DTO。

## AI 注意事项

- 新增列表接口时复制 **`Normalize()`** 模式，不要重新发明分页公式导致 off-by-one。
