# 组件规范

## 目录

- [布局](#布局)
- [通用组件](#通用组件)
- [页面](#页面)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 布局

- `components/layout/AppLayout.vue`：管理端壳层，嵌套 `RouterView`。
- 顶栏/侧栏：`AppTopNav.vue`、`AppSidebar.vue` 等与 `auth.menuTree` 配合。

## 通用组件

- 目录：`Web/src/components/common/`
- 示例：`TableActions.vue`、`PageSection.vue`、`IconPicker.vue` — 以 **展示与交互** 为主，不内嵌业务 API（由页面注入回调）。

## 页面

- 管理端页：`views/admin/*View.vue`
- 登录与 404：`LoginView.vue`、`NotFoundView.vue`

## 推荐方案

- 列表 + 编辑弹窗：状态提升在 **页面级**，子组件用 props/emit。
- 表格列与按钮权限：使用 **`PermissionCode`** 控制列操作显隐。

## 禁止事项

- 禁止在子组件内调用 **`useAuthStore` 发请求** 导致隐式重复请求（除非明确是全局轮询类需求）。

## AI 注意事项

- 新组件文件名 **PascalCase**；单文件组件默认 **具名导出 script setup** 无需 export default。
