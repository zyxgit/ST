# 日志规范（通用）

## 目录

- [后端事实](#后端事实)
- [结构化字段](#结构化字段)
- [级别指引](#级别指引)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 后端事实

- ST 后端使用 **NLog**（在 `AddSharedWebApi` 中清除默认日志提供程序并接入 NLog）；共享中有 **`RequestLoggingMiddleware`** 记录请求。
- 全局异常中间件对未捕获异常打 **`LogError`**，业务异常可按策略降级为 Warning（当前实现对部分异常注释了 Warning，以仓库代码为准）。

示例（控制器）：

```csharp
_logger.LogDebug("{Message}", message);
```

## 结构化字段

推荐使用占位符，避免字符串拼接：

```csharp
_logger.LogInformation("User {UserId} signed in from {Ip}", userId, ip);
```

## 级别指引

| 级别 | 用途 |
|------|------|
| `Trace`/`Debug` | 本地详细诊断 |
| `Information` | 正常业务里程碑 |
| `Warning` | 可恢复问题、重试、降级 |
| `Error` | 失败需告警或人工介入 |

## 推荐方案

- 请求入口/出口关键字段：`TraceIdentifier`（ProblemDetails 已暴露 `traceId`）、用户 id（若有）。
- 不在日志中输出 **密码、refresh token、完整 JWT**。

## 禁止事项

- 禁止在生产路径使用 `Console.WriteLine` 替代日志（`Program.cs` catch 中控制台输出仅作兜底时可保留现有模式）。
- 禁止记录完整银行卡号、证件号；如需排查使用掩码。

## AI 注意事项

- 新增中间件时，确保 **不重复记录 body** 导致日志膨胀；若参考 `RequestLoggingMiddleware`，注意 PII 与大小限制（参阅 `docs/ai/common/Architecture.md` 风险说明）。
- **OTLP 导出**已在 `AddSharedWebApi` 中条件启用：设置 `OTEL_EXPORTER_OTLP_ENDPOINT` 环境变量后自动加载 OpenTelemetry LoggerProvider（与 NLog 并存；NLog 写文件/控制台，OTel 写 OTLP）。
- 可观测性基础设施（Alloy → Loki → Grafana）部署与验证见 [`Observability.md`](./Observability.md)。
