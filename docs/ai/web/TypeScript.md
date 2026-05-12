# TypeScript 规范

## 目录

- [配置](#配置)
- [路径别名](#路径别名)
- [类型放置](#类型放置)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 配置

- `tsconfig.app.json` / `tsconfig.node.json` 分层；Vue SFC 由 Vite + vue-tsc 校验。

## 路径别名

`vite.config.ts`：

```typescript
resolve: {
  alias: {
    '@': fileURLToPath(new URL('./src', import.meta.url)),
  },
},
```

业务导入统一 **`@/...`**。

## 类型放置

- 领域类型：`src/types/*.ts`（如 `auth.ts`、`menu.ts`、`user.ts`）。
- API 响应：`LoginResult` 等放在 `types` 并在 `api/*.ts` 引用。

## 代码示例

```typescript
import type { MenuTreeNode } from '@/types/menu'

export async function getCurrentUserMenuTree(): Promise<MenuTreeNode[]> {
  return request.get('/identity/menu/tree')
}
```

## 推荐方案

- DTO 与后端对齐字段名；必要时 **`camelCase` 前端映射** 在 API 层做一次转换。
- `as const` 用于权限常量对象。

## 禁止事项

- 禁止 `// @ts-ignore` 覆盖大片代码。
- 禁止 `JSON.parse` 结果是 `any` 且无守卫。

## AI 注意事项

- 新增接口先在 **`types`** 定义再在 **`api`** 使用，避免重复匿名类型。
