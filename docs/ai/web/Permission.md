# 权限规范（前端）

## 目录

- [常量](#常量)
- [路由](#路由)
- [运行时校验](#运行时校验)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 常量

`Web/src/constants/permissions.ts`：

```typescript
export const PermissionCode = {
  UserQuery: 'system:user:query',
  UserCreate: 'system:user:create',
  // ...
} as const
```

与后端 JWT 中权限声明保持一致（格式 **模块:资源:动作**）。

## 路由

在 `routes.ts` 上挂 `meta.permission`：

```typescript
meta: { title: '用户管理', permission: PermissionCode.UserQuery },
```

## 运行时校验

- 全局：`router.beforeEach` 调用 `authStore.hasPermission`。
- 组件内：按钮级可用 `v-if="authStore.hasPermission(PermissionCode.UserCreate)"`。

## 代码示例

```typescript
const authStore = useAuthStore()
if (!authStore.hasPermission(PermissionCode.RoleQuery)) {
  // 隐藏或禁用
}
```

## 推荐方案

- 新增权限：**后端策略 + JWT 声明 + 前端常量 + 路由 meta** 四步同步。

## 禁止事项

- 禁止仅依赖前端隐藏敏感接口（**服务端必须校验**）。
- 禁止魔法字符串散落：`PermissionCode` 唯一真源。

## AI 注意事项

- 若后端使用 `perm:user:create` 风格 Policy，注意与 **`system:user:create`** 命名映射是否一致（项目现状以前端常量为准对齐后端）。
