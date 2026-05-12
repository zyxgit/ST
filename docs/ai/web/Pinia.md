# Pinia 规范

## 目录

- [事实](#事实)
- [auth Store](#auth-store)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 事实

- 使用组合式 Store：`defineStore('auth', () => { ... })`。
- 令牌读写封装在 `@/auth/token`，Store 同步内存状态。

## auth Store

职责（与 `Web/src/stores/auth.ts` 一致）：

- `bootstrap()`：并行拉取当前用户与菜单树 `getCurrentUser()`、`getCurrentUserMenuTree()`。
- `hasPermission(code)`：比对 JWT/后端返回的 `permissions` 数组。
- `login` / `logout`：与 API、路由跳转联动。

## 代码示例

```typescript
export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref(getAccessToken())
  const currentUser = ref<CurrentUser>({ isAuthenticated: false, roles: [], permissions: [] })
  const menuTree = ref<MenuTreeNode[]>([])
  const initialized = ref(false)

  async function bootstrap() {
    if (!getAccessToken()) {
      initialized.value = true
      return
    }
    const [user, menus] = await Promise.all([getCurrentUser(), getCurrentUserMenuTree()])
    currentUser.value = user
    menuTree.value = menus
    initialized.value = true
  }

  function hasPermission(permission?: string) {
    if (!permission) return true
    return currentUser.value.permissions.includes(permission)
  }

  return { accessToken, currentUser, menuTree, initialized, bootstrap, hasPermission /* ... */ }
})
```

## 推荐方案

- 会话级状态放 Pinia；**纯展示派生**用 `computed`。
- 跨标签页同步令牌不在范围内时保持现状（避免过度设计）。

## 禁止事项

- 禁止在 Store 外直接改 **localStorage 令牌** 而不走 `setTokens`/`clearTokens`。

## AI 注意事项

- 新增全局状态时评估是否应放入 **`auth`** 或新建 `defineStore`，避免上帝 Store。
