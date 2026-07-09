# API 路由规范与防 404 清单

本文用于指导新接口开发。目标是避免“服务已启动，但接口 404”的常见问题。

## 路由设计原则

1. **先确定外部路径，再确定下游路径。**
2. **Controller 路由和 Gateway Transform 必须一起设计。**
3. **不要凭感觉拼路径；必须用现有 Controller 和 Gateway 配置核对。**
4. **新增接口必须同时验证下游直连路径和 Gateway 外部路径。**

## 当前 Gateway 路径转换模式

大部分服务采用：

```text
外部路径：/api/<service-prefix>/{**catch-all}
Transform：PathRemovePrefix=/api/<service-prefix> + PathPrefix=/api
下游收到：/api/{**catch-all}
```

示例：

```text
外部：/api/identity/users
移除：/api/identity
添加：/api
下游：/api/users
```

因此，Identity 下游 Controller 应提供 `/api/users`，而不是 `/api/identity/users`。

## Controller 路由写法

推荐写法：在 Controller 上声明稳定资源前缀，在 Action 上声明相对路径。

```csharp
[ApiController]
[Route("api/catalog/products")]
public sealed class ProductsController : AbstractControllerBase
{
    [HttpGet]
    public Task<PagedResult<ProductDto>> GetListAsync(...)

    [HttpGet("{id:guid}")]
    public Task<ProductDto> GetAsync(Guid id)

    [HttpPost]
    public Task<Guid> CreateAsync(CreateProductRequest request)
}
```

也可以继承 `AbstractControllerBase`，但必须理解它已有：

```csharp
[ApiController]
[Authorize]
[Route("api/[controller]")]
```

如果继承该基类后在 Action 上写 `[HttpGet("api/products")]`，实际组合路径可能不是你以为的 `/api/products`。新接口优先在 Controller 上显式写 `[Route("api/xxx")]`，Action 只写相对片段。

## 404 高风险写法

避免以下模式：

```csharp
// 高风险：基类已有 api/[controller]，Action 又写完整 api 路径，容易组合出错误路径。
public sealed class ProductsController : AbstractControllerBase
{
    [HttpGet("api/products")]
    public Task<List<ProductDto>> GetAsync() => ...;
}
```

推荐改为：

```csharp
[Route("api/products")]
public sealed class ProductsController : AbstractControllerBase
{
    [HttpGet]
    public Task<List<ProductDto>> GetAsync() => ...;
}
```

如果确实需要绝对路由，必须使用 ASP.NET Core 支持的绝对模板并在代码评审中说明原因，例如 `~/api/products`；否则不要在 Action 上重复写 `api/...`。

## 新接口设计表

每个新接口开发前必须在任务说明或 PR 中写出：

| 项 | 示例 |
|----|------|
| 外部 Gateway 路径 | `POST /api/catalog/products` |
| Gateway Route | `/api/catalog/{**catch-all}` |
| Transform 后下游路径 | `POST /api/products` |
| Controller Route | `[Route("api/products")]` |
| Action Route | `[HttpPost]` |
| 是否鉴权 | 是，`perm:catalog:create` |
| 验证命令 | `curl -i http://localhost:<gateway>/api/catalog/products` |

## 404 排查顺序

1. 查看 OpenAPI/Scalar 中是否出现该接口。
2. 直接请求下游服务路径，确认 Controller 路由是否存在。
3. 请求 Gateway 外部路径，确认 Transform 后是否匹配下游路径。
4. 检查 Controller 是否继承 `AbstractControllerBase` 且重复写了 `api/...`。
5. 检查 HTTP Method 是否正确，避免 GET/POST 不匹配。
6. 检查 `{id:guid}` 等路由约束是否与传入值匹配。
7. 检查 Gateway route 是否被更早的 route 捕获。

## 新接口验收清单

- [ ] OpenAPI/Scalar 能看到接口。
- [ ] 下游直连路径返回非 404。
- [ ] Gateway 外部路径返回非 404/502。
- [ ] 权限不足时返回 401/403，而不是 404。
- [ ] 前端 API 路径与 Gateway 外部路径一致。
- [ ] 文档记录外部路径、下游路径、权限码和验证命令。
