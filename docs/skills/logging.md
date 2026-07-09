# logging skill

## 适用场景

NLog、OpenTelemetry、异常日志、操作日志、traceId、脱敏。

## 必须先读

- `docs/architecture/README.md`
- `docs/devops/README.md`

## 常用源码路径

- `Api/src/ServiceShared/ST.Shared.WebApi/Middleware/`
- `Api/src/Microservices/*/*Api/NLog/`
- `Api/src/Microservices/OperationLog/`
- `deploy/alloy/`
- `deploy/loki/`
- `deploy/grafana/`

## 开发规则

- 日志记录业务 ID、事件类型、traceId。
- 异常日志保留异常对象。
- 操作日志通过现有基础设施采集。

## 禁止事项

- 禁止输出完整 JWT、RefreshToken、密码、验证码、连接串。
- 禁止吞异常无日志。

## 不确定时必须询问

- 是否属于审计操作？
- 是否需要业务指标？
- 是否会包含敏感字段？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `git diff --check`
