# 文件上传与服务演进

## 目录

- [现状](#现状)
- [多存储架构（策略模式）](#多存储架构策略模式)
- [API 端点](#api-端点)
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

| 方法 | 路由 | 说明 |
|------|------|------|
| `POST` | `/api/files/upload` | 上传文件（multipart/form-data） |
| `GET` | `/api/files/{id}` | 获取文件元数据 |
| `DELETE` | `/api/files/{id}` | 删除文件 |
| `GET` | `/api/files/{id}/download` | 下载文件（需认证，返回文件流，不暴露存储路径） |
| `GET` | `/api/files/{id}/public/download` | **公开**下载文件（`[AllowAnonymous]`，仅 `FileAccessLevel.Public` 文件可用） |

上传返回的 URL 根据 `accessLevel` 动态决定：

- `Public` → `/api/files/{id}/public/download`（浏览器直接可打开）
- `Private` → `/api/files/{id}/download`（需带 Token）

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
- `GET /api/files/{id}/download` 端点受 `[Authorize]` 保护（继承自 `AbstractControllerBase`）
- `GET /api/files/{id}/public/download` 端点标记 `[AllowAnonymous]`，仅提供 `FileAccessLevel.Public` 的文件，服务层校验访问级别
- 上传返回的 URL 根据 `accessLevel` 自动选择对应端点，客户端直接使用无需额外判断

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
