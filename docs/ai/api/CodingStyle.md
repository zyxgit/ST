# C# 编码风格（ST）

## 目录

- [文件与命名空间](#文件与命名空间)
- [异步](#异步)
- [依赖注入](#依赖注入)
- [nullable](#nullable)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 文件与命名空间

- 一个公共类型一个文件；命名空间与文件夹路径一致。
- `internal` 仅在程序集内收敛实现细节。

## 异步

- 公开 API 使用 `async`/`await`；`ConfigureAwait(false)` 在库代码可选，在 ASP.NET Core 应用层通常省略。

```csharp
public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken ct)
{
	await using var cmd = await connection.CreateCommandAsync(ct);
	// ...
}
```

## 依赖注入

- 构造注入为主；避免 **服务定位器** 反模式（静态 `Resolve`）。

## nullable

- 启用 nullable context；引用类型显式 `?` 表示可空。

## 推荐方案

- `sealed` 类用于无继承设计的模块与服务。
- `record` 用于不可变 DTO。

## 禁止事项

- 禁止 `async void`（事件处理器除外）。
- 禁止捕获 `Exception` 后吞掉不记录。

## AI 注意事项

- 与 `.editorconfig`（`Api/src/.editorconfig`）保持一致缩进与换行（仓库 `.gitattributes` 为 CRLF）。
