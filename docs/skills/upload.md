# upload skill

## 适用场景

文件上传、下载、公开文件、签名 URL、分片上传、存储抽象。

## 必须先读

- `docs/backend/README.md`
- `docs/devops/README.md`

## 常用源码路径

- `Api/src/Microservices/FileUpload/`
- `Api/src/Microservices/FileUpload/ST.MS.FileUpload.Api/Controllers/`
- `Api/src/Microservices/FileUpload/ST.MS.FileUpload.Application/`

## 开发规则

- 文件元数据入库，文件内容走存储服务。
- 公开下载和签名下载必须有明确安全边界。
- 分片上传必须能查询状态、上传分片、完成合并、取消清理。

## 禁止事项

- 禁止信任客户端文件名作为存储路径。
- 禁止上传接口无大小限制。
- 禁止签名 URL 永不过期。

## 不确定时必须询问

- 文件是否公开？
- 最大文件大小和类型限制是什么？
- 是否需要对象存储兼容？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `git diff --check`
