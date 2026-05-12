# Vue Router 规范

## 目录

- [结构](#结构)
- [守卫](#守卫)
- [meta 约定](#meta-约定)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 结构

- `createWebHistory(import.meta.env.BASE_URL)`
- 路由拆分为 **`adminRoutes`** 与 **`publicRoutes`**（`Web/src/router/routes.ts`）。
- 汇总：`[...adminRoutes, ...publicRoutes]`（`index.ts`）。

## 守卫

`beforeEach` 流程（`Web/src/router/index.ts`）：

1. NProgress、文档标题。
2. 首次 `authStore.bootstrap()`（防并发 `bootstrapping`）。
3. `meta.public` → 放行。
4. 未登录 → `/login?redirect=`。
5. `meta.permission` → `authStore.hasPermission`，无权限 → `/dashboard`。

## meta 约定

| 字段 | 含义 |
|------|------|
| `public` | 无需登录 |
| `title` | 页面标题后缀 |
| `permission` | 所需权限码（字符串，与 `PermissionCode` 对齐） |

## 代码示例

```typescript
{
  path: 'system/users',
  name: 'users',
  component: () => import('@/views/admin/UsersView.vue'),
  meta: { title: '用户管理', permission: PermissionCode.UserQuery },
}
```

## 推荐方案

- 新增后台页：放在 `AppLayout` 的 `children` 下，与 **`PermissionCode`** 同步。

## 禁止事项

- 禁止绕过守卫在组件内假设“已登录”而不读 `authStore`。

## AI 注意事项

- 权限拦截失败默认去 **`/dashboard`**，不是 403 页（当前实现）；若产品要 403 需单独页与路由。
