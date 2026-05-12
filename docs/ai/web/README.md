# 前端（Web）AI 规范索引

本目录对齐 **`Web/src`** 现有实现：Vue 3 + TypeScript + Vite + Pinia + Vue Router + Axios + Naive UI。

## 文档列表

| 文档 | 主题 |
|------|------|
| [Vue.md](./Vue.md) | SFC、脚本 setup、组合式 API |
| [Pinia.md](./Pinia.md) | Store、`auth` bootstrap |
| [Router.md](./Router.md) | 路由表与守卫 |
| [Request.md](./Request.md) | Axios 封装与 401 刷新 |
| [Permission.md](./Permission.md) | 权限常量与路由 meta |
| [DynamicRoute.md](./DynamicRoute.md) | 菜单驱动与静态路由现状 |
| [Component.md](./Component.md) | 目录结构与组件边界 |
| [Hooks.md](./Hooks.md) | composable 约定 |
| [Style.md](./Style.md) | CSS、Naive UI |
| [TypeScript.md](./TypeScript.md) | 类型与 `@/` 别名 |
| [Env.md](./Env.md) | `VITE_*` 环境变量 |
| [CodingStyle.md](./CodingStyle.md) | ESLint/Prettier/Oxlint |
| [AI-Rules.md](./AI-Rules.md) | AI 约束清单 |

## 入口文件

- `Web/src/main.ts`
- `Web/src/App.vue`
- `Web/src/router/index.ts`、`routes.ts`
- `Web/src/stores/auth.ts`
- `Web/src/lib/request.ts`
