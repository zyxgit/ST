# DTO 规范

## 目录

- [放置](#放置)
- [命名](#命名)
- [校验](#校验)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 放置

- 输入/输出模型位于 **`*.Application/Dto`**（或项目约定子目录），命名清晰的 record/class。
- 不与 EF **实体**混用对外暴露（使用映射投影）。

## 命名

- 命令：`CreateUserCommand`、`UpdateMenuCommand`
- 查询结果：`UserListItemDto`、`MenuTreeNodeDto`（若与前端对齐可同名后缀）

## 校验

- 使用 **DataAnnotations** 或 FluentValidation（若项目已引用）；控制器动作参数自动 400。

示例：

```csharp
public sealed record CreateUserCommand(
	[Required][MaxLength(64)] string UserName,
	[EmailAddress] string Email
);
```

## 代码示例

分页请求复用：

```csharp
public sealed class UserQueryDto : PagedRequestDto
{
	public string? Keyword { get; set; }
}
```

## 推荐方案

- 写命令与读模型分离（CQRS 轻度采用）：`Commands/` vs `Queries/` 子文件夹。
- Mapster 配置集中在 **`MapsterConfig`**（`ST.Shared.Application` 已有入口）。

## 禁止事项

- 禁止 DTO 继承 **实体类**。
- 禁止把 **DbContext** 注入 DTO 或映射配置里做 IO。

## AI 注意事项

- 新增 DTO 后若前端有 TypeScript 类型，提示同步更新 `Web/src/types/`。
