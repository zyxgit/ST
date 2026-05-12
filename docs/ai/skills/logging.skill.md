# logging.skill

## 1. Skill Name

`st-logging-nlog` — NLog、请求日志与全局异常链。

## 2. Purpose

- 约束日志级别、结构化占位符、中间件顺序；禁止泄露敏感与大体 Body。

## 3. Tech Stack

| 项 | 事实 |
|----|------|
| 框架 | NLog（`AddSharedWebApi` 替换默认日志） |
| 中间件 | `GlobalExceptionMiddleware`、`RequestLoggingMiddleware` |
| 追踪 | `ProblemDetails.Extensions["traceId"]` = `HttpContext.TraceIdentifier` |
| 可观测 | OpenTelemetry：`AddServiceDefaults`（Aspire） |

## 4. Architecture Rules

- 顺序：`UseSharedWebApi` 内先全局异常，再请求日志，再 HTTPS/CORS/认证（见 `WebApplicationBuilderExtensions`）。
- 异常：`BusinessException` / `DomainException` / 其它 → ProblemDetails JSON。

## 5. Coding Rules

- 使用 `_logger.LogInformation("User {UserId} ...", userId)`，避免 `$"..."` 拼接敏感字段。
- 未处理异常：`LogError(ex, "...")` 保留异常对象。

## 6. Naming Rules

- 日志事件消息：中文业务可读 + 英文标识符字段名（与现有混排风格兼容）。

## 7. Best Practices

- 关联 `traceId` 与用户 id（脱敏）便于排查。
- 请求 Body 日志遵循现有中间件配置；敏感字段掩码。

## 8. Forbidden Practices

- `Info` 级别打印完整密码、Authorization 头、refresh token。
- 吞异常不记录堆栈。

## 9. AI Generation Constraints

- 新增中间件不得打乱 `UseSharedWebApi` 内认证前后顺序。
- 不在库代码滥用 `ConfigureAwait(false)` 除非已存在团队规范。

## 10. Example Code

```csharp
_logger.LogDebug("{Message}", message);
```

```csharp
problem.Extensions["traceId"] = context.TraceIdentifier;
```

## 11. Related Documents

- `docs/ai/api/Logging.md`
- `docs/ai/common/Logging.md`
- `docs/ai/common/Architecture.md`（请求体日志风险）
