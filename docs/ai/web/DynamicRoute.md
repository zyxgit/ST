# 动态路由与菜单

## 目录

- [当前实现](#当前实现)
- [菜单树](#菜单树)
- [与路由的关系](#与路由的关系)
- [代码示例](#代码示例)
- [演进](#演进)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 当前实现

- **路由表静态定义**在 `Web/src/router/routes.ts`（管理端 `adminRoutes`）。
- 权限由 **`meta.permission`** 控制可见与进入；**菜单数据**来自后端 `getCurrentUserMenuTree()`，在 `auth` Store 的 `menuTree` 中，用于 **顶部/侧边导航渲染**（`lib/admin-menu.ts`）。

## 菜单树

类型见 `@/types/menu` 中 **`MenuTreeNode`**；`admin-menu.ts` 将树转为 Naive UI `MenuOption`，并过滤 `isHide`、`type === 3` 等。

## 与路由的关系

- 动态菜单 **不自动注册新路由**；新页面仍需在 `routes.ts` 增加 `path` + `component`。
- `normalizePath` 保证 path 以 `/` 开头。

## 代码示例

构建侧栏（节选）：

```typescript
export function buildSideMenuOptions(menuTree: MenuTreeNode[]): MenuOption[] {
  return [
    { key: '/dashboard', label: '工作台' },
    ...mapTreeToMenuOptions(menuTree),
  ]
}
```

## 演进

若未来 **纯动态路由**（后端返回 component key）：需约定 **组件映射表**（安全沙箱）或使用 **模块联邦**，当前文档以 **静态路由 + 动态菜单** 为准。

## 推荐方案

- 新菜单项：后端配置 path 与前端路由 **path 完全一致**，避免 404。

## 禁止事项

- 禁止信任后端返回的任意 **组件路径字符串** 直接 `import()`（防止供应链风险）。

## AI 注意事项

- 加管理页面时同时更新 **菜单构建逻辑**（若新顶级区块）与 **`routes.ts`**。
