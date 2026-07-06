# auth skill

## 适用场景

JWT、RefreshToken、权限策略、菜单权限、用户上下文、租户上下文。

## 必须先读

- `docs/backend/README.md`
- `docs/frontend/README.md`

## 常用源码路径

- `Api/src/Microservices/Identity/`
- `Api/src/ServiceShared/ST.Shared.WebApi/Authentication/`
- `Api/src/ServiceShared/ST.Shared/Authentication/`
- `Web/src/router/`
- `Web/src/stores/`

## 开发规则

- 权限码采用 `perm:资源:动作`。
- 登录、刷新、登出必须考虑 token 生命周期和失效。
- 权限变更必须考虑缓存失效。
- 多租户操作必须保留租户上下文。

## 禁止事项

- 禁止提交 JWT SigningKey。
- 禁止日志输出完整 token、密码、验证码。
- 禁止前端只隐藏按钮而后端不鉴权。

## 不确定时必须询问

- 是否需要新增权限码？
- 权限属于菜单、按钮还是 API？
- 是否要租户级隔离？

## 验收检查

- `dotnet build Api/src/ST.slnx`
- `cd Web && pnpm build`
