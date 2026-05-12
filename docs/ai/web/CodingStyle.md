# 前端编码风格

## 目录

- [格式化](#格式化)
- [Lint](#lint)
- [Vue SFC](#vue-sfc)
- [命名](#命名)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 格式化

- Prettier：`Web/.prettierrc.json`
- EditorConfig：`Web/.editorconfig`

## Lint

- ESLint：`eslint.config.ts`
- Oxlint：`Web/.oxlintrc.json`

提交前执行：

```bash
cd Web && pnpm exec eslint . && pnpm exec oxlint .
```

（以 `package.json` 脚本为准若已封装。）

## Vue SFC

- 顺序推荐：**`<script setup>` → `<template>` → `<style scoped>`**。
- 模板中使用 **`camelCase` props** 与脚本一致。

## 命名

- 组件：**PascalCase**
- 函数/变量：**camelCase**
- 常量：**UPPER_SNAKE** 或 `as const` 对象

## 推荐方案

- API 函数动词开头：`getUsers`、`createRole`。
- 布尔变量：`isLoading`、`hasError`。

## 禁止事项

- 禁止无意义的缩写：`usrMgr`。
- 禁止在模板写复杂表达式（抽到 computed）。

## AI 注意事项

- 变更后跑 **`pnpm build`** 确保类型与打包通过。
