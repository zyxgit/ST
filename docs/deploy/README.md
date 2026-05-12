# 部署与运行导航

## 本地开发（与仓库一致）

### 构建后端

在 Monorepo 根目录：

```bash
dotnet build Api/src/ST.slnx
```

在 `Api/` 目录下：

```bash
dotnet build src/ST.slnx
```

### 推荐：Aspire 启动多服务

```bash
dotnet run --project Api/src/Aspire/ST.Aspire.AppHost/ST.Aspire.AppHost.csproj
```

### 单服务调试

```bash
dotnet run --project Api/src/Microservices/Identity/ST.MS.Identity.Api/ST.MS.Identity.Api.csproj
```

```bash
dotnet run --project Api/src/Microservices/Test/ST.MS.Test.Api/ST.MS.Test.Api.csproj
```

```bash
dotnet run --project Api/src/Microservices/FileUpload/ST.MS.FileUpload.Api/ST.MS.FileUpload.Api.csproj
```

### 网关

网关项目路径：`Api/src/Microservices/Gateway/ST.Gateway/ST.Gateway.csproj`。生产/预发前置反向代理、TLS 终端、YARP 路由与限流策略；配置节 `ReverseProxy`、`DownstreamServices:*:Address` 等需按环境覆盖（勿将生产密钥提交仓库）。

### 前端

```bash
cd Web
pnpm install
pnpm dev
```

根目录环境变量见 `Web` 下 Vite 约定（`VITE_*`），详见 [`../ai/web/Env.md`](../ai/web/Env.md)。

## 配置与密钥

- 共享配置加载顺序与 UserSecrets/环境变量键名约定见 [`../ai/api/ServiceTemplate.md`](../ai/api/ServiceTemplate.md) 与 [`../ai/api/Auth.md`](../ai/api/Auth.md)。
- 禁止在仓库中保存生产 `Jwt:SigningKey`、数据库连接串、SMTP 密码等；使用环境变量或密钥管理器。

## AI 注意

- 部署清单、K8s、Helm 等可在此目录下按环境追加子文档；**与 `docs/ai/common/Monorepo.md` 的目录约定不冲突**即可。
