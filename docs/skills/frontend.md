# frontend skill

## 适用场景

Vue 页面、路由、Pinia、Axios、权限菜单、组件。

## 必须先读

- `docs/ai/README.md`
- `docs/frontend/README.md`

## 常用源码路径

- `Web/src/router/`
- `Web/src/stores/`
- `Web/src/lib/request.ts`
- `Web/src/api/`
- `Web/src/views/`
- `Web/src/components/`

## 开发规则

- HTTP 调用走统一请求层。
- API 类型和后端 DTO 对齐。
- 路由 meta、菜单、权限码同步。
- 组件 props/emits 明确类型。

## 禁止事项

- 禁止组件中直接创建临时 Axios。
- 禁止绕过权限守卫。
- 禁止把 token 或密码打印到日志。

## 不确定时必须询问

- 菜单是否由后端动态下发？
- 权限码是什么？
- 接口路径是否经过 Gateway？

## 验收检查

- `cd Web && pnpm build`
- `git diff --check`
