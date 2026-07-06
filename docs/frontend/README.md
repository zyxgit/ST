# 前端开发规范

## 技术栈

- Vue 3
- TypeScript
- Vite
- Pinia
- Vue Router
- Naive UI
- Axios

## 目录约定

| 路径 | 说明 |
|------|------|
| `Web/src/main.ts` | 应用入口 |
| `Web/src/router/` | 路由、守卫、权限 meta |
| `Web/src/stores/` | Pinia 状态 |
| `Web/src/lib/request.ts` | Axios 实例、Token、错误处理 |
| `Web/src/api/` | 按业务域拆分 API 调用 |
| `Web/src/views/` | 页面 |
| `Web/src/components/` | 通用组件 |

## API 调用

- 所有 HTTP 调用必须走统一请求封装，不在组件中直接创建临时 Axios 实例。
- API 文件按业务域命名，如 `user.ts`、`role.ts`、`operation-log.ts`。
- 新增后端接口时同步更新对应前端 API 类型和调用方法。
- Token、401 刷新、错误提示由统一请求层处理，页面只处理业务状态。

## 路由与权限

- 路由 meta 必须声明标题、权限或公开访问策略。
- 需要菜单动态渲染的页面必须和 Identity 菜单/权限码保持一致。
- 权限码风格与后端策略保持一致：`perm:资源:动作`。
- 路由守卫不得写复杂业务逻辑，复杂逻辑放入 store 或服务函数。

## Pinia 状态

- 会话、用户信息、菜单树、全局应用状态放入 store。
- store action 负责异步加载和状态落地，组件避免重复请求相同全局数据。
- 不将敏感 token 长期暴露在日志或调试输出中。

## 组件规范

- 页面组件负责组合；通用行为抽为 components 或 composables。
- 表格、筛选、分页、弹窗、表单校验尽量复用现有模式。
- 组件 props/emits 使用 TypeScript 明确类型。
- 样式优先局部化，避免全局选择器污染。

## 环境变量

- Vite 环境变量必须以 `VITE_` 开头。
- API 基址通过 `VITE_API_BASE_URL` 或现有约定配置。
- 新增环境变量必须同步 `docs/devops/README.md`。

## 前端变更清单

- [ ] API 类型与后端 DTO 对齐。
- [ ] 路由、菜单、权限码同步。
- [ ] 页面错误提示与加载状态完整。
- [ ] `cd Web && pnpm build` 通过，若环境限制需说明。
- [ ] 文档同步更新。
