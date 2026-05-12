# Vue 3 规范

## 目录

- [形态](#形态)
- [目录](#目录)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 形态

- **Vue 3 + `<script setup lang="ts">`** 为主。
- 页面置于 `src/views/`，可复用块置于 `src/components/`。

## 目录

```
Web/src/
├── views/           # 页面级
├── components/      # 通用与布局组件
├── stores/          # Pinia
├── router/
├── api/             # 后端接口封装
├── lib/             # 工具与第三方封装
├── types/
└── styles/
```

## 代码示例

根组件挂载（节选）：

```vue
<script setup lang="ts">
import { RouterView } from 'vue-router'
</script>

<template>
  <RouterView />
</template>
```

## 推荐方案

- 列表页：**Naive UI** `DataTable` + 独立加载状态；错误用 `useDiscrete().message`。
- 路由懒加载：`() => import('@/views/admin/UsersView.vue')`（与 `routes.ts` 一致）。

## 禁止事项

- 禁止在组件内直接 `new Axios()` 绕过 `lib/request.ts`。
- 禁止 `any` 大面积掩盖类型（除与第三方临时交界）。

## AI 注意事项

- 新建页面必须注册路由：`router/routes.ts` 的 `adminRoutes` 或 `publicRoutes`。
