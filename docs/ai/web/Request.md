# HTTP 请求封装（Axios）

## 目录

- [事实](#事实)
- [基址与超时](#基址与超时)
- [认证](#认证)
- [401 刷新](#401-刷新)
- [错误提示](#错误提示)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 事实

- 单例：`Web/src/lib/request.ts`
- 响应拦截器 **直接返回 `response.data`**（类型泛型第二参数与返回一致）。

## 基址与超时

```typescript
const instance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL + '/api',  // 自动追加 /api 满足网关路由
  timeout: 20000,
})
```

## 认证

请求拦截器注入 **`Authorization: Bearer <accessToken>`**（从 `@/auth/token` 读取）。

## 401 刷新

- 对同一请求防重入：设置 header **`x-refresh-retry`**。
- 刷新端点：`POST ${VITE_API_BASE_URL}/identity/user/refresh`（`buildApiUrl` 拼接）。
- 失败：`clearTokens()` 并 `router.replace('/login')`。

## 错误提示

优先顺序：`data.message` → `data.detail` → `data.title` → `error.message`。

```typescript
const errorMessage =
  error.response?.data?.message ??
  error.response?.data?.detail ??
  error.response?.data?.title ??
  error.message ??
  '请求失败，请稍后重试'

message.error(errorMessage)
```

## 代码示例

业务调用：

```typescript
const request = {
  get<T>(url: string, config?: object) {
    return instance.get<T, T>(url, config)
  },
  post<T>(url: string, data?: unknown, config?: object) {
    return instance.post<T, T>(url, data, config)
  },
}
export default request
```

## 推荐方案

- API 模块按域拆分：`api/auth.ts`、`api/user.ts`，内部使用默认 `request`。

## 禁止事项

- 禁止在新代码使用 **`fetch`** 混合两套错误处理（除非封装进同一层）。
- 禁止硬编码完整域名而不走 **`VITE_API_BASE_URL`**。

## AI 注意事项

- 后端 ProblemDetails 字段与前端读取顺序必须一致（参见 `docs/ai/api/Exception.md`）。
