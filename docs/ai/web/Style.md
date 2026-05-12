# 样式规范

## 目录

- [技术栈](#技术栈)
- [全局样式](#全局样式)
- [与 Naive UI](#与-naive-ui)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 技术栈

- 普通 CSS / 组件内 `<style scoped>`；基础样式在 `Web/src/styles/base.css`、`nprogress.css`。

## 全局样式

- `main.ts` 引入全局样式；避免过高优先级破坏 Naive 主题。

## 与 Naive UI

- 使用组件 props 与 **全局主题**（若后续接入 `NConfigProvider`）优于深度选择器覆盖。
- 需要穿透时使用 `:deep()`：

```vue
<style scoped>
.page :deep(.n-data-table-th) {
  font-weight: 600;
}
</style>
```

## 推荐方案

- 间距与排版：优先 Naive 内置 **`space`**、**`grid`**；减少魔法像素。
- 暗色主题若启用：与 Naive 变量一并切换（演进项）。

## 禁止事项

- 禁止全局 `!important` 覆盖组件库导致维护地狱。
- 禁止内联样式承载主题 token（应变量化）。

## AI 注意事项

- 管理后台保持 **简洁中性色**，关键操作用 `type="error"` / `primary` 按钮语义。
