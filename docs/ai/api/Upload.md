# 文件上传与服务演进

## 目录

- [现状](#现状)
- [多存储架构（策略模式）](#多存储架构策略模式)
- [API 端点](#api-端点)
- [分片上传](#分片上传)
- [文件访问控制](#文件访问控制)
- [文件类型与大小校验](#文件类型与大小校验)
- [ASP.NET Core 标准写法](#aspnet-core-标准写法)
- [网关与大小限制](#网关与大小限制)
- [配置方式](#配置方式)
- [扩展新存储](#扩展新存储)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 现状

当前仓库包含独立的 **FileUpload** 微服务（`Api/src/Microservices/FileUpload/`），支持：

- 三种存储后端：本地 / MinIO / OSS（配置切换）
- 文件访问级别：公开 / 私有
- 文件类型白名单 + 文件大小限制（配置控制）
- 统一 API 下载（不暴露存储路径）

### 分层

| 层 | 职责 | 关键文件 |
|----|------|----------|
| `*.Domain` | 端口 + 枚举 + 配置 | `IFileStorageService`、`FileStorageType`、`FileStorageOptions`、`FileAccessLevel`、`FileEntity` |
| `*.Infra` | 各存储适配器 | `LocalFileStorageService`、`MinIOFileStorageService`、`OSSFileStorageService` |
| `*.Application` | 业务编排、校验 | `FileAppService` — 注入 `IFileStorageService` + `IOptions<FileStorageOptions>` |
| `*.Api` | 启动注册、过滤器 | `Program.cs` + `FileUploadValidationFilter` |

### Application 层不感知存储实现

`FileAppService` 只依赖 `IFileStorageService` 接口，切换存储类型无需修改 Application 层：

```csharp
public sealed class FileAppService : AbstractAppService, IFileAppService
{
    private readonly IFileStorageService _storageService;

    public async Task<FileUploadResultDto> UploadAsync(Stream stream, string fileName, string contentType, FileAccessLevel accessLevel = FileAccessLevel.Private)
    {
        var filePath = await _storageService.UploadAsync(stream, fileName, contentType);
        var entity = new FileEntity(fileName, filePath, stream.Length, contentType, extension, accessLevel);
        // ... 落库 ...
    }
}
```

## 多存储架构（策略模式）

三种存储实现统一实现 `IFileStorageService`：

| 实现 | 命名空间 | 说明 |
|------|----------|------|
| `LocalFileStorageService` | `Infra.Services` | 本地磁盘，按 `yyyy/MM/dd` 分目录，GUID 文件名 |
| `MinIOFileStorageService` | `Infra.Services` | MinIO 对象存储（模拟），生产需接入 `Minio` NuGet 包 |
| `OSSFileStorageService` | `Infra.Services` | 阿里云 OSS（模拟），生产需接入 `Aliyun.OSS.SDK.NetCore` NuGet 包 |

### 工厂注册

在 `Program.cs` 中通过配置选择实现，对 DI 容器透明：

```csharp
builder.Services.AddSingleton<IFileStorageService>(sp =>
{
    var options = sp.GetRequiredService<IOptions<FileStorageOptions>>();
    return options.Value.Type switch
    {
        FileStorageType.MinIO => ActivatorUtilities.CreateInstance<MinIOFileStorageService>(sp),
        FileStorageType.OSS => ActivatorUtilities.CreateInstance<OSSFileStorageService>(sp),
        _ => ActivatorUtilities.CreateInstance<LocalFileStorageService>(sp)
    };
});
```

> **注意**：各实现类**不再**标记 `ITransientDependency`（避免 Autofac 自动注册干扰），生命周期由 `Program.cs` 中的 `AddSingleton` 统一管理。

## API 端点

### 普通上传

| 方法 | 路由 | 说明 |
|------|------|------|
| `GET` | `/api/files` | 文件列表分页查询（支持按文件名、访问级别、MIME 类型筛选） |
| `POST` | `/api/files/upload` | 上传文件（multipart/form-data，自动计算 SHA256 Hash） |
| `GET` | `/api/files/{id}` | 获取文件元数据 |
| `DELETE` | `/api/files/{id}` | 删除文件（仅上传者可删除） |
| `GET` | `/api/files/{id}/download` | 下载文件（Private 文件仅上传者可下载） |
| `GET` | `/api/files/{id}/public/download` | **公开**下载文件（`[AllowAnonymous]`，仅 `FileAccessLevel.Public` 文件可用） |

### 分片上传

| 方法 | 路由 | 说明 |
|------|------|------|
| `POST` | `/api/files/multipart/init` | 初始化分片上传 |
| `GET` | `/api/files/multipart/{uploadId}/status` | 查询上传状态（断点续传） |
| `POST` | `/api/files/multipart/{uploadId}/chunks/{chunkIndex}` | 上传单个分片 |
| `POST` | `/api/files/multipart/{uploadId}/complete` | 完成上传（触发合并） |
| `POST` | `/api/files/multipart/check-by-hash` | 秒传检查 |
| `DELETE` | `/api/files/multipart/{uploadId}` | 取消上传 |

### 签名下载 URL

| 方法 | 路由 | 说明 |
|------|------|------|
| `POST` | `/api/files/signed-url` | 生成签名下载 URL |
| `GET` | `/api/files/signed/{token}?sig={signature}` | 通过签名 URL 下载文件（无需认证） |

上传返回的 URL 根据 `accessLevel` 动态决定：

- `Public` → `/api/files/{id}/public/download`（浏览器直接可打开）
- `Private` → `/api/files/{id}/download`（需带 Token）

### 文件列表查询

```json
GET /api/files?pageIndex=1&pageSize=20&keyword=report&accessLevel=0&contentType=image/

{
  "pageIndex": 1,
  "pageSize": 20,
  "totalCount": 42,
  "items": [
    {
      "id": "xxx",
      "fileName": "report.pdf",
      "fileSize": 1048576,
      "contentType": "application/pdf",
      "extension": ".pdf",
      "accessLevel": 0,
      "url": "/api/files/xxx/public/download",
      "createTime": "2026-06-24T10:00:00Z",
      "uploaderName": "admin"
    }
  ]
}
```

| 参数 | 类型 | 说明 |
|------|------|------|
| `pageIndex` | int | 页码（默认 1） |
| `pageSize` | int | 每页条数（默认 20，最大 100） |
| `keyword` | string | 文件名模糊搜索 |
| `accessLevel` | int | 按访问级别筛选（0=Public, 1=Private） |
| `contentType` | string | 按 MIME 类型前缀筛选（如 `image/`） |

### 文件 Hash 计算

普通上传 `POST /api/files/upload` 在存储文件的同时通过 `SHA256 CryptoStream` 自动计算文件 Hash，存储到 `files.file_hash` 字段。这使得秒传检查 `POST /api/files/multipart/check-by-hash` 能同时匹配普通上传和分片上传的文件。

分片上传的 Hash 由客户端在 `POST /multipart/init` 时通过 `fileHash` 字段提供。

## 分片上传

支持大文件分片上传、断点续传、秒传和异步合并。

### 文件类型校验

`POST /multipart/init` 在创建上传会话前校验：

- **文件扩展名**：与 `FileStorage.AllowedExtensions` 白名单比对
- **MIME 类型**：若客户端提供了 `contentType` 字段，与 `FileStorage.AllowedContentTypes` 白名单比对
- **文件大小**：不超过 `FileStorage.MaxFileSize`

校验失败返回 `BusinessException`。这与普通上传的 `FileUploadValidationFilter` 共用同一套白名单配置。

### 数据模型

```
file_upload_sessions              # 上传会话
├── id                            # 会话 ID
├── file_name                     # 原始文件名
├── file_hash                     # 文件 SHA256（秒传用）
├── file_size                     # 文件总大小
├── chunk_size                    # 分片大小
├── total_chunks                  # 总分片数
├── uploaded_chunks               # 已上传分片数
├── status                        # Uploading/Merging/Completed/Failed/Expired
├── access_level                  # 访问级别（0=Public, 1=Private）
├── created_by                    # 上传用户 ID
├── file_id                       # 合并后的文件 ID
└── expires_at_utc                # 过期时间

file_upload_chunks                # 分片记录
├── id                            # 分片 ID
├── upload_id                     # 关联会话 ID
├── chunk_index                   # 分片序号（从 0 开始）
├── chunk_hash                    # 分片 SHA256
├── size                          # 分片大小
└── storage_path                  # 存储路径
```

### 上传流程

```
1. 客户端                          2. 服务端
   │                                  │
   ├─ POST /multipart/init ─────────→│ 创建会话，返回 uploadId + totalChunks
   │                                  │
   ├─ POST /multipart/{id}/chunks/0 ─→│ 上传分片 0
   ├─ POST /multipart/{id}/chunks/1 ─→│ 上传分片 1
   ├─ ...                             │ ...
   ├─ POST /multipart/{id}/chunks/N ─→│ 上传分片 N
   │                                  │
   ├─ POST /multipart/{id}/complete ─→│ 触发异步合并
   │                                  │
   └─ GET /multipart/{id}/status ───→│ 查询合并进度
```

**init 请求参数**：

| 字段 | 类型 | 必填 | 说明 |
|------|------|:----:|------|
| `fileName` | string | ✅ | 原始文件名 |
| `fileSize` | long | ✅ | 文件总大小（字节） |
| `chunkSize` | int | — | 分片大小（默认 5MB） |
| `fileHash` | string | — | 文件 SHA256（秒传用） |
| `contentType` | string | — | MIME 类型（用于白名单校验） |
| `accessLevel` | int | — | 访问级别（0=Public, 1=Private，默认 1） |

### 断点续传

上传中断后，通过 `status` 接口查询已上传的分片：

```json
GET /api/files/multipart/{uploadId}/status

{
  "uploadId": "xxx",
  "totalChunks": 20,
  "uploadedChunks": 8,
  "uploadedChunkIndexes": [0, 1, 2, 3, 4, 5, 6, 7],
  "missingChunkIndexes": [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19],
  "status": "Uploading"
}
```

客户端只需上传 `missingChunkIndexes` 中的分片。

### 秒传

上传前先检查文件 Hash。服务端同时查询 `file_upload_sessions`（已完成的分片上传）和 `files` 表（普通上传的文件），任意一方匹配即返回已有文件信息：

```json
POST /api/files/multipart/check-by-hash
{
  "fileHash": "sha256:abc123...",
  "fileSize": 104857600
}

{
  "exists": true,
  "fileId": "existing-file-id",
  "fileName": "video.mp4",
  "fileSize": 104857600
}
```

如果 `exists: true`，直接使用已有文件，无需重复上传。

**去重覆盖范围**：
- 通过分片上传完成的文件（`file_upload_sessions.Status = Completed`）
- 通过普通上传的文件（`files.file_hash` 匹配）

> 注意：普通上传的文件 Hash 需要在上传时由客户端或服务端计算并存储到 `files.file_hash` 字段。

### 异步合并

`POST /complete` 接口将上传会话标记为 `Merging` 状态后立即返回，实际合并由 `MultipartMergeService` 后台服务异步执行。

**合并流程**：

```
1. 客户端                          2. 服务端
   │                                  │
   ├─ POST /multipart/{id}/complete ─→│ 校验分片完整性
   │  ← { status: "merging" }        │ 标记状态为 Merging
   │                                  │ 清理 Redis 键
   │                                  │
   │  （后台服务 PeriodicTimer 轮询）  │
   │                                  ├─ 扫描 Merging 状态会话
   │                                  ├─ ConcatenatedReadStream 串联分片流（流式，不占内存）
   │                                  ├─ 流式上传合并后的文件
   │                                  ├─ 创建 FileEntity 记录
   │                                  ├─ 更新状态为 Completed
   │                                  │
   ├─ GET /multipart/{id}/status ───→│ 返回 Completed + FileId
```

**配置**（`appsettings.json`）：

```json
{
  "MultipartMerge": {
    "Enabled": true,
    "PollingIntervalSeconds": 10,
    "BatchSize": 5,
    "MaxRetryCount": 3
  }
}
```

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `Enabled` | 是否启用后台合并服务 | `true` |
| `PollingIntervalSeconds` | 轮询间隔（秒） | `10` |
| `BatchSize` | 每批处理的会话数量 | `5` |
| `MaxRetryCount` | 单个会话最大重试次数 | `3` |

**重试机制**：合并失败的会话保持 `Merging` 状态，下次轮询自动重试。超过 `MaxRetryCount` 次后标记为 `Failed`，错误信息记录在 `session.ErrorMessage` 中。

### 幂等性

- 重复上传同一分片（相同 `uploadId` + `chunkIndex`）返回成功
- 不会产生脏数据或重复计数

### 过期清理

- 上传会话 24 小时后过期
- 过期的会话状态变为 `Expired`
- 建议通过后台任务定期清理过期会话和分片文件

### Redis 键空间

分片上传使用 Redis Set 记录已上传分片，提升断点续传查询性能：

| 键模式 | 类型 | TTL | 说明 |
|--------|------|-----|------|
| `file:upload:{uploadId}:chunks` | Set | 24h | 已上传分片序号集合 |
| `file:upload:{uploadId}:chunks:init` | String | 24h | 初始化标记 |

**示例**：
```
file:upload:550e8400-e29b-41d4-a716-446655440000:chunks
  → Set: [0, 1, 2, 3, 4, 5, 6, 7]
```

**优势**：
- 查询已上传分片：O(1) 复杂度，无需查询数据库
- 幂等检查：`SISMEMBER` 命令，性能优于数据库查询
- 自动过期：24 小时后自动清理，无需手动维护

## 签名下载 URL

私有文件生成短期有效的下载链接，无需暴露存储路径。

### 使用流程

```
1. 客户端                          2. 服务端
   │                                  │
   ├─ POST /files/signed-url ────────→│ 生成签名 URL
   │  { fileId, expiresIn }          │ 返回: { url, expiresAt }
   │                                  │
   ├─ 分享 URL 给第三方 ─────────────→│
   │                                  │
   │  第三方访问签名 URL              │
   ├─ GET /files/signed/{token} ─────→│ 验证签名 + 过期时间
   │  ?sig=xxx                       │ 返回文件流
```

### 生成签名 URL

```json
POST /api/files/signed-url
{
  "fileId": "550e8400-e29b-41d4-a716-446655440000",
  "expiresIn": 3600
}

{
  "url": "https://host/api/files/signed/abc123?sig=xyz789",
  "expiresAtUtc": "2026-06-23T12:00:00Z",
  "expiresIn": 3600
}
```

### 签名算法

- **算法**：HMAC-SHA256
- **负载格式**：`{fileId}:{expiresAtTicks}:{userId}`
- **签名密钥**：配置项 `SignedUrl:SecretKey`

```
签名 URL 结构：
/api/files/signed/{Base64UrlEncode(payload)}?sig={HMAC-SHA256(payload, secretKey)}
```

### 安全特性

| 特性 | 说明 |
|------|------|
| 时效性 | 默认 1 小时，最大 24 小时 |
| 防篡改 | HMAC-SHA256 签名验证 |
| 绑定用户 | 签名包含用户 ID（可选） |
| 不暴露路径 | 令牌中不包含存储路径 |

### 配置

```json
{
  "SignedUrl": {
    "SecretKey": "your-secret-key-change-in-production",
    "BaseUrl": "https://your-domain.com"
  }
}
```

> ⚠️ **启动校验**：`SecretKey` 必须配置且不能为占位符（以 `CHANGE-ME` 开头），否则服务启动时抛出 `InvalidOperationException`。生产环境请使用环境变量或 User Secrets 配置。

### 限制

- 签名 URL 最大有效期：24 小时
- 令牌过期后无法使用
- 签名被篡改后验证失败
- 不支持范围下载（Range Requests）

## 文件访问控制

`FileEntity.AccessLevel` 由 `FileAccessLevel` 枚举控制：

```csharp
public enum FileAccessLevel
{
    Public = 0,   // 任意认证用户可下载
    Private = 1   // 需特定权限或资源归属
}
```

- 上传时可指定 `accessLevel` 参数（表单字段），默认值来自配置 `DefaultAccessLevel`
- `GET /api/files/{id}/download` 端点受 `[Authorize]` 保护，**Private 文件仅上传者可下载**
- `GET /api/files/{id}/public/download` 端点标记 `[AllowAnonymous]`，仅提供 `FileAccessLevel.Public` 的文件，服务层校验访问级别
- `DELETE /api/files/{id}` 端点**仅上传者可删除**（校验 `CreateBy == userId`）
- 上传返回的 URL 根据 `accessLevel` 自动选择对应端点，客户端直接使用无需额外判断
- 分片上传通过 `init` 请求的 `accessLevel` 字段指定，合并后的 `FileEntity` 继承该值

## 文件类型与大小校验

通过 `FileUploadValidationFilter`（`IActionFilter`）在控制器层拦截校验：

```csharp
public sealed class FileUploadValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // 1. 检查文件大小（FileStorageOptions.MaxFileSize）
        // 2. 检查 MIME 类型白名单（AllowedContentTypes）
        // 3. 检查文件扩展名白名单（AllowedExtensions）
    }
}
```

校验规则全部来自配置，修改 `appsettings.json` 的 `FileStorage` 节即可调整：

```json
{
  "FileStorage": {
    "AllowedContentTypes": ["image/jpeg", "image/png", "application/pdf"],
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".pdf"],
    "MaxFileSize": 10485760,
    "DefaultAccessLevel": "Private"
  }
}
```

白名单为空数组时表示不限制该维度。

## ASP.NET Core 标准写法

控制器接收 `multipart/form-data`：

```csharp
[HttpPost("upload")]
[RequestSizeLimit(200 * 1024 * 1024)] // 200MB 硬上限，实际限制由配置中的 MaxFileSize 控制
[ServiceFilter<FileUploadValidationFilter>]
public async Task<IActionResult> Upload(IFormFile file, [FromForm] FileAccessLevel? accessLevel = null)
{
    if (file is null || file.Length == 0)
        throw new BusinessException("请选择要上传的文件");

    await using var stream = file.OpenReadStream();
    var result = await _fileAppService.UploadAsync(stream, file.FileName, file.ContentType, accessLevel ?? _options.DefaultAccessLevel);
    return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
}
```

下载端点直接返回 `File` 结果，由 ASP.NET Core 处理流释放：

```csharp
[HttpGet("{id:guid}/download")]
public async Task<IActionResult> Download(Guid id)
{
    var result = await _fileAppService.DownloadAsync(id);
    return File(result.Stream, result.ContentType, result.FileName);
}
```

## 网关配置（YARP）

FileUpload 服务已注册到 ST.Gateway（YARP），通过反向代理统一对外暴露。

### 路由规则

| 路由名 | 匹配路径 | 目标前缀 | 说明 |
|--------|----------|----------|------|
| `fileupload-api-route` | `/api/files/{**catch-all}` | 直通（不裁剪） | API 请求直接转发到上游，控制器 `[Route("api/files")]` 处理 |
| `fileupload-docs-route` | `/docs/fileupload/{**catch-all}` | 移除 `/docs/fileupload` | OpenAPI/Scalar 文档 |

> **注意**：`fileupload-api-route` 不裁剪前缀，因为上游 `FileController` 的 `[Route("api/files")]` 已包含 `/api/files` 路径。如果裁剪前缀会导致路由不匹配（上游收到 `/upload` 而非 `/api/files/upload`），产生 502 错误。

### Cluster 配置

```json
{
  "ReverseProxy": {
    "Clusters": {
      "fileupload-cluster": {
        "Destinations": {
          "fileupload-destination": {
            "Address": "https://localhost:7250"
          }
        }
      }
    }
  }
}
```

### 下游地址覆盖

通过 `DownstreamServices:FileUpload:Address` 环境变量或配置覆盖，用于 Aspire 编排动态分配端口：

```json
{
  "DownstreamServices": {
    "FileUpload": {
      "Address": "https://localhost:7250"
    }
  }
}
```

### 限流

FileUpload API 使用全局 `gateway-proxy` 策略（默认 120 req/60s），文档端点使用 `gateway-local-docs` 策略。

### 新增服务时的网关调整

新建微服务时，须在 Gateway 中完成：

1. **`appsettings.json`** — 添加 `DownstreamServices:服务名:Address`、`ReverseProxy:Routes`（API 路由 + Docs 路由）、`ReverseProxy:Clusters`
2. **`Program.cs`** — `ApplyGatewayDestinationOverrides` 添加目标地址映射；`ResolveRequestScope` 添加路径匹配分支；添加 `/docs/服务名` 重定向并配置限流
3. **`wwwroot/docs/index.html`** — 新增服务卡片（含 Scalar + OpenAPI 链接）

详见 [`ServiceTemplate.md`](./ServiceTemplate.md#网关注册)。

## 配置方式

完整配置示例 `appsettings.json`：

```json
{
  "FileStorage": {
    "Type": "Local",
    "UploadRoot": "uploads",
    "AllowedContentTypes": [
      "image/jpeg",
      "image/png",
      "image/gif",
      "application/pdf"
    ],
    "AllowedExtensions": [
      ".jpg",
      ".jpeg",
      ".png",
      ".gif",
      ".pdf"
    ],
    "MaxFileSize": 10485760,
    "DefaultAccessLevel": "Private"
  }
}
```

## 扩展新存储

1. 在 `Domain/FileStorageType.cs` 添加枚举成员
2. 在 `Infra/Services/` 下创建新类实现 `IFileStorageService`
3. 在 `Program.cs` 的工厂 `switch` 中添加分支
4. 更新 `appsettings.json` 配置使用新类型

## 推荐方案

- 大文件：**直传对象存储（预签名 URL）**，API 只签发凭证与落库元数据。
- 病毒扫描与 MIME 校验在 **后台任务**（Hangfire）异步完成。
- 开发环境用 Local，测试/预发用 MinIO，生产用 OSS/MinIO。

## 可观测性指标

FileUpload 服务注册了自定义 OpenTelemetry 指标（Meter: `ST.FileUpload`），在 `FileUploadMetrics.cs` 中定义。

### 指标列表

| 指标名 | 类型 | 说明 |
|--------|------|------|
| `st_fileupload_count_total` | Counter | 上传成功数 |
| `st_fileupload_failed_total` | Counter | 上传失败数 |
| `st_fileupload_size_bytes` | Histogram | 文件大小分布 (bytes) |

### 埋点位置

| 方法 | 指标 |
|------|------|
| `FileAppService.UploadAsync` | count + size_bytes |

### Grafana Dashboard

- **ST - 全局总览**：`deploy/grafana/provisioning/dashboards/st-overview.json`

## 禁止事项

- 禁止将上传根路径设在 **Web 根目录可执行区**。
- 禁止信任客户端提供的 **`Content-Type`** 作为唯一校验（应与服务端白名单配合）。
- 禁止在 `LocalFileStorageService` 上恢复 `ITransientDependency`（由工厂统一注册）。
- 禁止在 Application 层直接引用 `Infra.Services` 命名空间。
- 禁止客户端直接访问存储路径（统一通过 `/api/files/{id}/download`）。
- 禁止在 `LocalFileStorageService` 中信任 `filePath` 参数导致路径穿越。必须使用 `Path.GetFullPath` 解析规范路径并校验其仍在上传根目录内：`if (!fullPath.StartsWith(rootDir, ...)) throw UnauthorizedAccessException`。

## 操作日志（OperationLog）

通过 `[OperationLog]` 属性标记文件操作的审计日志，由 `OperationLogActionFilter` 自动捕获。

### 记录规则

| 端点 | 操作名 | RecordRequest | RecordResponse | 原因 |
|------|--------|:---:|:---:|------|
| `POST /api/files/upload` | `"文件上传"` | ✅ | ✅ | 审计谁上传了什么文件 |
| `DELETE /api/files/{id}` | `"删除文件"` | ✅ | ❌ | 审计谁删除了什么文件 |
| `GET /api/files/{id}/download` | `"下载文件"` | ✅ | ❌ | 审计谁访问了文件（避免序列化大流） |
| `GET /api/files/{id}` | 不记录 | — | — | 只读元数据查询，不记录 |
| `GET /api/files/{id}/public/download` | 不记录 | — | — | 公开下载高频，避免日志膨胀 |

### 开发约束

1. **新增文件操作端点** 必须同步判定是否需要 `[OperationLog]`，遵循上表分级：
   - 写操作（上传、删除、修改）→ 必须记录
   - 读操作→只读元数据不记录；文件内容下载视敏感度决定
   - 公开端点考虑日志量级，高频公开端点可不记录
2. **下载端点** `RecordResponse` 始终设为 `false`，避免序列化文件流。
3. `RecordRequest` 安全：`OperationLogActionFilter` 自动将 `IFormFile` 参数序列化为 `"<file>"`，不会序列化二进制内容。
4. 操作名统一为 `"文件XX"` 格式，每个端点唯一。

## AI 注意事项

- 生成上传接口时必须附带 **`RequestSizeLimit`** 或全局限制说明。
- 元数据存 EF 时记录 **`TenantId`**（多租户预留，见 `common/MultiTenant.md`）。
- 新增存储类型时同步更新 `FileStorageType` 枚举和 `Program.cs` 的工厂 `switch`。
- 修改验证规则时更新 `FileStorageOptions` 配置属性，保持与 `appsettings.json` 和文档一致。
- 新增 `FileAccessLevel` 枚举值时需同步更新过滤器与下载端点的访问检查逻辑。
- 上传返回 URL 统一使用 `/api/files/{id}/download` 或 `/api/files/{id}/public/download` 格式，不暴露具体存储路径。Public 文件用公开端点，Private 文件用需认证端点。
- 本地存储实现中，`DeleteAsync` 和 `GetStreamAsync` 必须做路径穿越防护：`Path.GetFullPath` + `StartsWith(rootDir)` 校验，阻止 `..` 逃逸上传根目录。
