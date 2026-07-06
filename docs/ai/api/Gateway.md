# Gateway 规范

## 目录

- [概述](#概述)
- [目录结构](#目录结构)
- [CorrelationId 中间件](#correlationid-中间件)
- [限流配置](#限流配置)
- [限流规则](#限流规则)
- [Redis 键空间](#redis-键空间)
- [配置示例](#配置示例)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 概述

Gateway 是 ST 项目的 API 网关，基于 YARP（Yet Another Reverse Proxy）实现，提供：

- 统一路由与反向代理
- CORS 跨域支持
- ForwardedHeaders 转发头处理
- 分布式限流（支持 InMemory/Redis 两种模式）
- OpenAPI / Scalar 文档入口

## 目录结构

```
ST.Gateway/
├── RateLimiting/
│   ├── GatewayRateLimitOptions.cs      # 限流配置模型
│   ├── RateLimitingMiddleware.cs       # 限流中间件
│   └── RateLimitingExtensions.cs       # DI 扩展方法
├── Program.cs                          # 入口
├── appsettings.json                    # 配置文件
└── ST.Gateway.csproj
```

## 限流配置

限流功能通过 `appsettings.json` 中的 `RateLimiting` 节配置：

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Enabled` | bool | true | 是否启用限流 |
| `Mode` | string | "InMemory" | 限流模式：`InMemory`（单机）或 `Redis`（分布式） |
| `DefaultApiPermitLimit` | int | 120 | 默认 API 限流（请求数/窗口） |
| `DefaultAuthPermitLimit` | int | 20 | 默认 Auth 限流（请求数/窗口） |
| `DefaultDocsPermitLimit` | int | 240 | 默认 Docs 限流（请求数/窗口） |
| `DefaultWindowSeconds` | int | 60 | 默认窗口大小（秒） |
| `Rules` | array | [] | 自定义限流规则列表 |

## 限流规则

自定义规则按顺序匹配，第一个匹配的规则生效。

| 字段 | 类型 | 说明 |
|------|------|------|
| `Name` | string | 规则名称（用于日志和 Redis 键） |
| `PathPrefix` | string? | 路径前缀匹配（如 "/api/identity/user/login"） |
| `PermitLimit` | int | 窗口内允许的最大请求数 |
| `WindowSeconds` | int | 限流窗口大小（秒） |
| `PartitionBy` | string | 分区维度：Ip, User, Path, IpPath, UserPath, Tenant, TenantUser, TenantPath |
| `HttpMethod` | string? | HTTP 方法过滤（null 表示不限制） |

### 分区维度说明

| 维度 | 说明 | 适用场景 |
|------|------|----------|
| `Ip` | 按客户端 IP | 登录、注册等防刷接口 |
| `User` | 按用户 ID | 普通 API 接口 |
| `Path` | 按请求路径 | 全局限流 |
| `IpPath` | 按 IP + 路径 | 特定接口防刷 |
| `UserPath` | 按用户 + 路径 | 用户级接口限流 |
| `Tenant` | 按租户 ID（JWT claim `tid`） | 租户级 QPS 限制 |
| `TenantUser` | 按租户 + 用户 | 租户内用户级限流 |
| `TenantPath` | 按租户 + 路径 | 租户特定接口限流 |

## Redis 键空间

分布式限流使用 Redis Sorted Set 实现滑动窗口：

```
rate:{ruleName}:{partitionKey}
```

示例：
- `rate:auth-login:ip:192.168.1.1` — 登录接口按 IP 限流
- `rate:file-upload:user:uuid-123` — 文件上传按用户限流
- `rate:api-default:ip:192.168.1.1` — 默认 API 按 IP 限流
- `rate:tenant-api:tenant:abc123` — 租户级全局限流
- `rate:tenant-orders:tenant:abc123:user:uuid-456` — 租户内用户级限流

**TTL**：等于限流窗口大小，过期后自动清理。

## 配置示例

### 基础配置

```json
{
  "RateLimiting": {
    "Enabled": true,
    "Mode": "Redis",
    "DefaultApiPermitLimit": 120,
    "DefaultAuthPermitLimit": 20,
    "DefaultDocsPermitLimit": 240,
    "DefaultWindowSeconds": 60
  }
}
```

### 自定义规则

```json
{
  "RateLimiting": {
    "Enabled": true,
    "Mode": "Redis",
    "Rules": [
      {
        "Name": "auth-login",
        "PathPrefix": "/api/identity/user/login",
        "PermitLimit": 10,
        "WindowSeconds": 60,
        "PartitionBy": "Ip"
      },
      {
        "Name": "auth-register",
        "PathPrefix": "/api/identity/user/register",
        "PermitLimit": 5,
        "WindowSeconds": 60,
        "PartitionBy": "Ip"
      },
      {
        "Name": "file-upload",
        "PathPrefix": "/api/files",
        "PermitLimit": 30,
        "WindowSeconds": 60,
        "PartitionBy": "User"
      }
    ]
  }
}
```

### 租户级限流配置

```json
{
  "RateLimiting": {
    "Enabled": true,
    "Mode": "Redis",
    "Rules": [
      {
        "Name": "tenant-api",
        "PermitLimit": 1000,
        "WindowSeconds": 60,
        "PartitionBy": "Tenant"
      },
      {
        "Name": "tenant-orders",
        "PathPrefix": "/api/orders",
        "PermitLimit": 100,
        "WindowSeconds": 60,
        "PartitionBy": "TenantUser"
      },
      {
        "Name": "tenant-auth",
        "PathPrefix": "/api/identity/user/login",
        "PermitLimit": 20,
        "WindowSeconds": 60,
        "PartitionBy": "TenantPath"
      }
    ]
  }
}
```

> **注意**：`Tenant` 维度从 JWT claim `tid` 提取租户 ID。未携带 `tid` 的请求归入 `tenant:anonymous` 分区。

## DI 注册

```csharp
// Program.cs
builder.Services.AddGatewayRateLimiting(builder.Configuration);
builder.Services.AddRedisInfra(builder.Configuration);
builder.Services.AddRedisRateLimiting();

// 中间件
app.UseRateLimiter();           // ASP.NET Core 内置限流（用于本地 docs）
app.UseGatewayRateLimiting();   // Gateway 分布式限流
```

## CorrelationId 中间件

Gateway 新增 CorrelationId 中间件，用于全链路请求关联。

### 工作流程

```
客户端请求
    │
    ▼
Gateway CorrelationId 中间件
    ├─ 读取请求头 X-Correlation-Id
    │   ├─ 有 → 使用该值
    │   └─ 无 → 从 traceparent 提取 TraceId
    │           └─ 仍无 → 生成新的 GUID
    │
    ├─ 存入 HttpContext.Items["CorrelationId"]
    ├─ 写入响应头 X-Correlation-Id
    │
    ▼
YARP 转发到下游服务（自动携带 X-Correlation-Id）
```

### 链路关联

- **W3C traceparent**：由 .NET OTel SDK 自动生成，YARP 自动透传
- **X-Correlation-Id**：业务级关联 ID，由 Gateway 中间件管理
- 两者互补：traceparent 用于分布式追踪系统，X-Correlation-Id 用于业务日志关联

### 下游服务

下游服务无需额外配置：
- `Activity.Current.TraceId` 自动从 traceparent 恢复
- `X-Correlation-Id` 可从请求头读取用于日志输出

## 禁止事项

- 禁止在限流规则中硬编码 IP 或用户 ID
- 禁止关闭限流功能上线生产环境
- 禁止将 `DefaultApiPermitLimit` 设置过高（建议不超过 1000）

## AI 注意事项

- 新增限流规则时，同步更新 `appsettings.json` 和本文档
- 限流模式切换（InMemory/Redis）通过配置实现，无需改代码
- Redis 不可用时，`RedisRateLimiter` 会抛异常，中间件应降级为放行
- 日志中应记录被限流的 IP、用户、路径信息，便于排查
