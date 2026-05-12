# 后端日志（NLog / 中间件）

## 目录

- [事实](#事实)
- [管道顺序](#管道顺序)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 事实

- `AddSharedWebApi` 配置 **NLog**（替换默认日志）；各服务 `Program.cs` 模板含 `LogManager.Shutdown()`。
- **`RequestLoggingMiddleware`** 在 `GlobalExceptionMiddleware` 之后注册，记录请求（注意 body 大小与敏感数据，参见 `docs/ai/common/Architecture.md` 风险说明）。

## 管道顺序

摘自 `WebApplicationBuilderExtensions` 思路：

1. 安全响应头
2. **`GlobalExceptionMiddleware`**
3. **`RequestLoggingMiddleware`**
4. HTTPS、CORS、认证授权、控制器

## 代码示例

控制器调试日志：

```csharp
_logger.LogDebug("{Message}", message);
```

全局异常写 ProblemDetails（节选）：

```csharp
problem.Extensions["traceId"] = context.TraceIdentifier;
```

## 推荐方案

- 关联 **OpenTelemetry**（`AddServiceDefaults`）实现跨服务 trace（Aspire 场景）。

## 禁止事项

- 禁止在 Info 级别打印 **完整请求体**（含密码）。
- 禁止删除异常链 **`ex`** 直接 `LogError("error")` 无上下文。

## AI 注意事项

- 新增中间件时插入顺序须遵守 **`UseSharedWebApi`** 契约，不要随意 `UseMiddleware` 打乱认证前后关系。
