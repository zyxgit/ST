# 可观测性 — OpenTelemetry 日志链路

## 目录

- [架构概览](#架构概览)
- [日志管道](#日志管道)
- [组件说明](#组件说明)
- [基础设施配置](#基础设施配置)
- [后端集成](#后端集成)
- [Grafana 查询](#grafana-查询)
- [二阶段规划](#二阶段规划)
- [AI 注意事项](#ai-注意事项)

## 架构概览

```
.NET 微服务
  │  NLog → 控制台 / 本地文件
  │  OpenTelemetry SDK → OTLP gRPC (:4317)
  ▼
Grafana Alloy
  │  otelcol.receiver.otlp "default"
  │  otelcol.exporter.loki "default"
  ▼
Loki
  │  TSDB 存储，保留 7 天
  ▼
Grafana
  │  预置 Loki 数据源
  │  匿名只读访问
```

## 日志管道

| 环节 | 技术 | 说明 |
|------|------|------|
| 采集 | NLog + `Microsoft.Extensions.Logging` | 后端通过 `ILogger<T>` 输出，NLog 写入文件和控制台 |
| 结构化 | OpenTelemetry .NET SDK | `AddOpenTelemetry()` 挂载 `OpenTelemetryLoggerProvider` |
| 传输 | OTLP gRPC | 通过 `OTEL_EXPORTER_OTLP_ENDPOINT` 环境变量启用，发给 Alloy:4317 |
| 转发 | Alloy `otelcol.exporter.loki` | 将 OTLP 日志转换为 Loki 条目 |
| 存储 | Loki TSDB | 单实例文件系统存储，默认保留 7 天 |
| 展示 | Grafana Explore | 预置 Loki 数据源，LogQL 查询 |

## 组件说明

### Grafana Alloy

- 镜像：`grafana/alloy:v1.8.1`
- 配置：[`deploy/alloy/config.alloy`](../../deploy/alloy/config.alloy)
- OTLP gRPC 端口 `4317`（容器内），宿主映射 `24317`
- `stage.otel{}` 自动将 OTel resource attributes 转为 Loki label（如 `service_name`）
- 二阶段预留 Prometheus metrics pipeline（注释状态）

### Loki

- 镜像：`grafana/loki:3.4.2`
- 配置：[`deploy/loki/loki-config.yaml`](../../deploy/loki/loki-config.yaml)
- 端口 `3100`
- 文件系统存储，数据持久卷 `loki-data`
- TSDB schema，保留 168h（7 天）

### Grafana

- 镜像：`grafana/grafana:11.5.2`
- 端口 `3000`，宿主映射 `23000`
- 预置 Loki 数据源：[`deploy/grafana/datasources/loki.yaml`](../../deploy/grafana/datasources/loki.yaml)
- 默认凭据：`admin / admin123`（通过 `.env` 配置）
- 匿名访问已启用（Viewer 角色）

## 基础设施配置

见 [`deploy/docker-compose.yml`](../../deploy/docker-compose.yml) 中 `alloy` / `loki` / `grafana` 服务定义。

所有微服务均已注入环境变量：
```yaml
OTEL_EXPORTER_OTLP_ENDPOINT: http://alloy:4317
```

## 后端集成

启用条件（代码在 `WebApplicationBuilderExtensions.cs`）：

```csharp
if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });

    builder.Services.AddOpenTelemetry()
        .WithMetrics(metrics => { ... })
        .WithTracing(tracing => { ... })
        .UseOtlpExporter();
}
```

- `AddOpenTelemetry()` 增加 `OpenTelemetryLoggerProvider`，与 NLog 并存
- NLog 负责控制台 + 文件；OTel 负责 OTLP 导出
- Metrics 和 Tracing 也一并启用（但二期才正式接存储）

## Grafana 查询

### LogQL 示例

```logql
# 按服务名
{service_name="st-ms-identity-api"}

# 按级别
{service_name="st-ms-identity-api"} |= "Error"

# 按 TraceId（LogQL v2）
{service_name=~".+"} | json | TraceId=`abc123`
```

### 日志字段

Loki label 来自 OTel resource attributes（`stage.otel{}` 自动映射）：

| Loki Label | OTel Resource Attribute | 示例 |
|------------|------------------------|------|
| `service_name` | `service.name` | `st-ms-identity-api` |
| `service_instance_id` | `service.instance.id` | UUID |
| `source` | 外部 label | `st-alloy` |

日志正文（`LogLine`）包含 `Timestamp`、`Body`、`SeverityText`、`TraceId` 等标准 OTel 字段。

## 二阶段规划

| 阶段 | 内容 | 关键工作 |
|------|------|----------|
| 一期（本期） | 日志链路 OTLP → Alloy → Loki → Grafana | docker-compose + Alloy config + OTLP env |
| 二期 | Prometheus metrics + 告警 | Alloy batch processor → Prometheus remote write |
| 三期 | Tempo trace 链路 + 联动 | Alloy traces → Tempo + Loki derived fields |

## AI 注意事项

- 新增微服务时必须在 `docker-compose.yml` 中注入 `OTEL_EXPORTER_OTLP_ENDPOINT`
- 日志内容遵循 [`Logging.md`](./Logging.md) 规范（禁止输出密码/Token/PII）
- 修改 `nlog.base.config` 时注意 NLog 与 OTel 双轨输出，避免日志重复
- 新增环境变量需同步更新 `deploy/.env.example` 和 `docs/deploy/README.md`
