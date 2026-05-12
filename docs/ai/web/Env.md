# 环境变量（Vite）

## 目录

- [前缀](#前缀)
- [现有变量](#现有变量)
- [路径约定](#路径约定)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)

## 前缀

- Vite 仅暴露 **`VITE_`** 前缀到客户端代码。

## 现有变量

| 变量 | 用途 | 说明 |
|------|------|------|
| `VITE_API_BASE_URL` | Axios `baseURL`，也用于头像等静态资源 URL 拼接 | **必填**，网关地址，无默认值 |

请求直接发往网关，不经过 Vite proxy。`request.ts` 会自动在 `VITE_API_BASE_URL` 后追加 `/api` 以满足网关路由要求：

```
浏览器 → {VITE_API_BASE_URL}/api/{service-name}/api/{controller-path} → Gateway → 下游微服务
```

定义示例 `.env.local`（勿提交，仅本地开发）：

```
VITE_API_BASE_URL=http://localhost:5099
```

> 网关地址以实际部署的 Gateway 端口为准。本地开发时查看 Gateway 启动输出或 launchSettings.json。

## 路径约定

前端 API 路径格式：`/{service-name}/api/{controller-path}`。`request.ts` 的 `baseURL` 拼接为 `{VITE_API_BASE_URL}/api`，请求经过网关时：

```
前端 → {VITE_API_BASE_URL}/api/{service-name}/api/{controller-path}
        ↓
网关 PathRemovePrefix /api/{service-name}
        ↓
服务端收到 /api/{controller-path} → 匹配 Controller 路由
```

示例：前端请求路径 `/identity/api/user/login`，实际发出 `http://localhost:5099/api/identity/api/user/login` → 网关截掉 `/api/identity` → 转发 `/api/user/login` → 匹配 `UserController`。

## 代码示例

```typescript
baseURL: import.meta.env.VITE_API_BASE_URL
```

## 推荐方案

- 环境区分：`.env.development`、`.env.production`；敏感值不入前端 env。
- 生产 API 根路径由部署平台注入 **`VITE_API_BASE_URL`**。

## 禁止事项

- 禁止把 **`JWT SigningKey`**、数据库密码写入 `VITE_*`。
