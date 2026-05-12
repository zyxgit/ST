# frontend.skill

## 1. Skill Name

`st-frontend-vue` — ST Monorepo 前端（`Web/src`）开发与约束。

## 2. Purpose

- 统一 Axios、路由、权限、Pinia、类型放置，避免第二套 HTTP 客户端与权限魔法字符串。
- 对齐管理端（Naive UI）与网关路径。

## 3. Tech Stack

| 项 | 事实 |
|----|------|
| 框架 | Vue 3 + `<script setup lang="ts">` |
| 构建 | Vite（`vite.config.ts`，`@` → `src`） |
| 状态 | Pinia（`stores/auth.ts` 等） |
| 路由 | Vue Router，`routes.ts` 拆 `adminRoutes` / `publicRoutes` |
| HTTP | 单例 `src/lib/request.ts`（Axios） |
| UI | Naive UI + `lib/naive.ts` `useDiscrete()` |

## 4. Architecture Rules

- 页面：`src/views/`；布局与通用：`src/components/`；API 封装：`src/api/*.ts`；类型：`src/types/*.ts`。
- 路由：`router/index.ts` 汇总；`beforeEach` 里 `authStore.bootstrap()`、未登录跳转 `/login`、`meta.permission` 校验。
- 鉴权数据：`getAccessToken` / `setTokens` / `clearTokens`（`auth/token`）；权限列表在 `auth` store。
- 环境：`import.meta.env.VITE_API_BASE_URL` **必填**，网关地址；无默认值，不经过 Vite proxy。

## 5. Coding Rules

- 业务请求一律 `import request from '@/lib/request'`，禁止新建 `axios.create` 用于业务。
- 新页面必须在 `adminRoutes` 或 `publicRoutes` 注册；需权限则 `meta: { permission: PermissionCode.xxx }`。
- 权限码：唯一来源 `src/constants/permissions.ts` 的 `PermissionCode`，与后端声明对齐。
- 错误展示：拦截器已 `message.error`；页面避免重复弹同一错误。

## 6. Naming Rules

- 组件文件：PascalCase（`UsersView.vue`）。
- API 函数：动词 + 名词（`getUsers`、`createRole`）。
- Store：`useXxxStore`；路由 `name` 小写短横线路径一致。

## 7. Best Practices

- 列表页：loading + 空状态；表格操作列与 `hasPermission` 联动。
- 类型：API 返回先在 `types` 定义再用于 `api/*.ts`。
- 路由懒加载：`() => import('@/views/...')` 与现有一致。

## 8. Forbidden Practices

- 第二套 Axios 实例或混用未封装的 `fetch` 调业务 API。
- 手写权限字符串散落组件（绕过 `PermissionCode`）。
- 在 `setup` 外直接改 localStorage 令牌而不走 `setTokens`/`clearTokens`。

## 9. AI Generation Constraints

- 新增 API：必须同步 `@/types` 与 `@/api`；确认网关 `appsettings.json` 中已配置对应 `DownstreamServices` 和 `ReverseProxy` 路由。
- 新增权限：同时列后端 Policy 与 `PermissionCode` 枚举项。
- 路由 `meta.public: true` 仅用于登录页、404 等；后台页勿滥用。
- 文档：`docs/ai/web/*`、`DocumentationSync.md`。

## 10. Example Code

```typescript
// lib/request.ts — 拦截器已返回 response.data
import request from '@/lib/request'
import type { MenuTreeNode } from '@/types/menu'

export function getCurrentUserMenuTree() {
  return request.get<MenuTreeNode[]>('/identity/menu/tree')
}
```

```typescript
// router/routes.ts — 权限 meta
{
  path: 'system/users',
  component: () => import('@/views/admin/UsersView.vue'),
  meta: { title: '用户管理', permission: PermissionCode.UserQuery },
}
```

```typescript
// stores/auth.ts — 校验
function hasPermission(permission?: string) {
  if (!permission) return true
  return currentUser.value.permissions.includes(permission)
}
```

## 11. Related Documents

- `docs/ai/web/README.md`、`Router.md`、`Request.md`、`Permission.md`、`Env.md`、`Vue.md`、`Pinia.md`
