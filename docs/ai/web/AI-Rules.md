# 前端 AI 生成规则（强制对齐）

## 目录

- [真源](#真源)
- [必须遵守](#必须遵守)
- [生成前检查清单](#生成前检查清单)
- [禁止](#禁止)

## 真源

- `docs/ai/web/*.md`、`Web/src` 现有代码。

## 必须遵守

1. HTTP：**仅用** `Web/src/lib/request.ts`，环境与 **`VITE_API_BASE_URL`** 一致。
2. 路由：新页注册于 **`router/routes.ts`**，并配置 **`meta.permission`**（若需权限）。
3. 权限：使用 **`PermissionCode`**，禁止手写散落字符串。
4. 状态：会话与用户菜单走 **`useAuthStore`**。
5. UI：管理端默认 **Naive UI**，离散 API 用 **`useDiscrete()`**。
6. 错误：后端 ProblemDetails → Axios 拦截器已 **`message.error`**，页面无需双重弹同样错误（除非补充上下文）。
7. **文档**：路由、权限、请求、环境变量或交互模式变化时，按 [`../common/DocumentationSync.md`](../common/DocumentationSync.md) 更新 `docs/ai/web/*.md` 及关联索引；必要时新增专题文件并在 `docs/ai/web/README.md` 登记。

## 生成前检查清单

- [ ] 是否需新增 `PermissionCode` 与后端 Policy？
- [ ] API 路径是否与网关/Vite 代理一致？
- [ ] 类型是否落在 `@/types`？
- [ ] 是否已更新相关 **`docs/ai/web/**/*.md`**（及 `docs/deploy` 若代理/基址变化）？

## 禁止

- 禁止新建第二套 Axios 实例。
- 禁止在未授权路由组件内假设菜单已加载而不判断 **`initialized`**。
- 禁止引入与本项目无关的 UI 框架并列使用（除非架构变更评审）。
- 禁止**仅改前端**而不更新权限/路由/请求相关规范文档（当行为对团队或 Agent 可见时）。
