# 异常与 ProblemDetails

## 目录

- [全局中间件](#全局中间件)
- [异常类型](#异常类型)
- [响应格式](#响应格式)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 全局中间件

`ST.Shared.WebApi/Middleware/GlobalExceptionMiddleware.cs`：

- `BusinessException` → 使用异常内 **`StatusCode`**，ProblemDetails `Title` 为「业务异常」，扩展 **`errorCode`**。
- `DomainException` → HTTP **400**，「验证失败」。
- 其它 `Exception` → HTTP **500**，详细信息读取配置 **`App:ErrorMessage`**。

## 异常类型

```csharp
public sealed class BusinessException : Exception
{
	public int StatusCode { get; }
	public string? ErrorCode { get; }
	public BusinessException(string message, int statusCode = 400, string? errorCode = null);
}
```

## 响应格式

- `Content-Type: application/problem+json`
- 扩展字段：`traceId`；业务异常可选 `errorCode`。

## 代码示例

测试控制器（真实代码）：

```csharp
[HttpGet("bussiness")]
public IActionResult BusinessError()
{
	throw new BusinessException("你是憨批蛮", errorCode: "RVFGFG_ASDADSA");
}
```

## 推荐方案

- 可预期错误：**BusinessException**；输入校验：**DomainException** 或 FluentValidation（若引入）。
- 国际化：消息可由应用层根据错误码映射（演进项）。

## 禁止事项

- 禁止用 **500** 表达已知业务规则失败。
- 禁止向前端返回 **堆栈跟踪**（开发环境亦应谨慎）。

## AI 注意事项

- 前端 Axios 拦截器读取 **`detail` / `title` / `message`**（见 `Web/src/lib/request.ts`），后端应保持 ProblemDetails 字段一致。
