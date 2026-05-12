# 对外 API 与集成说明（入口）

> 本文件是 **人类/集成方** 的快速入口；**AI 编码规范** 以 [`../ai/api/`](../ai/api/) 为准。

## 身份与权限

- JWT：签发与校验见各服务 `Jwt` 配置；声明与 `IUserContext` 见 [`../ai/api/Auth.md`](../ai/api/Auth.md)。
- 权限策略：`perm:资源:动作` 形式（与代码中 `Authorize(Policy = ...)` 一致）。

## 错误格式

- 业务与未处理异常统一由 `GlobalExceptionMiddleware` 输出 **`application/problem+json`**，含 `traceId`，业务异常可带 `errorCode` 扩展。详见 [`../ai/api/Exception.md`](../ai/api/Exception.md)。

## 网关

- 外部流量经 YARP 网关（`ST.Gateway`）转发至各微服务；路径前缀与集群配置见 `ReverseProxy` 配置节。前端 `Web` 一般通过 `VITE_API_BASE_URL` 指向网关或具体服务，见 [`../ai/web/Request.md`](../ai/web/Request.md)。

## 服务列表（代码中的微服务示例）

- Identity、OperationLog、Test 等位于 `Api/src/Microservices/`；具体路由以各 `*.Api` 中 Controller 与 OpenAPI 为准。

## 深入

- 完整后端规范：[`../ai/api/README.md`](../ai/api/README.md)
