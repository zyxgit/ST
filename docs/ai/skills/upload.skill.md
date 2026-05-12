# upload.skill

## 1. Skill Name

`st-upload-files` — 文件上传与对象存储演进的约束（仓库当前以 JSON API 为主）。

## 2. Purpose

- 约束 ASP.NET Core 接收 `IFormFile`、大小限制、网关与反向代理一致；避免 Agent 生成未限制体或不可部署路径。

## 3. Tech Stack

| 项 | 说明 |
|----|------|
| 传输 | `multipart/form-data`，`IFormFile` |
| 主机 | Kestrel + 可选 YARP 网关 + 上游 Nginx/Ingress |
| 存储 | 演进：直传 S3/兼容对象存储 + 元数据表（与现 EF 体系一致） |

## 4. Architecture Rules

- 上传端点放在具体微服务 `*.Api`；**不**经前端直连存储桶密钥。
- 大文件优先 **预签名 URL** 直传，API 只负责策略与元数据落库。
- 文件下载分两个端点：`{id}/download`（需认证）和 `{id}/public/download`（`[AllowAnonymous]`，仅限 Public 文件）。上传返回 URL 根据 `FileAccessLevel` 自动选择。

## 5. Coding Rules

- Action 使用 `[RequestSizeLimit(bytes)]` 或全局 `FormOptions` / Kestrel `MaxRequestBodySize`。
- 校验：扩展名 + 魔数 + 大小；不信任客户端 `Content-Type` 唯一性。
- 保存路径：应用数据目录或云 URI，**禁止**可执行 Web 根目录。

## 6. Naming Rules

- 端点动词：`UploadAvatar`、`ImportUsers`；存储对象 key：`{tenant?}/{service}/{yyyyMM}/{guid}.{ext}`（演进一致即可）。

## 7. Best Practices

- 病毒扫描/异步处理放 Hangfire（`IBackgroundTaskScheduler`）。
- 元数据与审计：用户 id、原文件名、大小、hash、创建 UTC。

## 8. Forbidden Practices

- 无限制 `IFormFile` 接收。
- 将云存储 **AccessKey** 写入前端或仓库配置明文。
- 本地文件存储路径不经 `Path.GetFullPath` + `StartsWith(rootDir)` 校验即拼接，导致路径穿越漏洞。

## 9. AI Generation Constraints

- 每个上传 Action 必须带 **显式大小上限** 或引用全局配置键名。
- 若加 `IFormFile` 新依赖，列迁移/部署对网关 `client_max_body_size` 影响。

## 10. Example Code

```csharp
[Authorize]
[HttpPost("files")]
[RequestSizeLimit(10_485_760)]
public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
{
	if (file == null || file.Length == 0)
		throw new DomainException("请选择文件");
	await using var read = file.OpenReadStream();
	// 存储或推送到对象存储
	return Ok(new { length = file.Length, name = file.FileName });
}
```

## 11. Related Documents

- `docs/ai/api/Upload.md`
- `docs/ai/api/Hangfire.md`
- `docs/deploy/README.md`
