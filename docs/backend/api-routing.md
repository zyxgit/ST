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

## 返回类型规范

为了确保 Swagger / Scalar 能正确显示响应 DTO，Controller 方法的返回类型必须遵循以下规则：

### 核心原则

**能用 `ActionResult<T>` 就不用 `IActionResult`。**

```csharp
// ✅ 正确 - Swagger 自动识别返回类型
public async Task<ActionResult<OrderDto>> GetOrder(Guid id) { ... }

// ❌ 错误 - Swagger 无法识别返回类型
public async Task<IActionResult> GetOrder(Guid id) { ... }
```

### 各场景返回类型

| 场景 | 返回类型 | 示例 |
|------|---------|------|
| 返回 DTO | `ActionResult<T>` | `ActionResult<OrderDto>` |
| 返回列表 | `ActionResult<List<T>>` | `ActionResult<List<SkuDto>>` |
| 返回分页 | `ActionResult<PagedResultDto<T>>` | `ActionResult<PagedResultDto<OrderDto>>` |
| 返回布尔/ID | `ActionResult<bool>` / `ActionResult<Guid>` | `ActionResult<bool>` |
| 返回文件流 | `IActionResult` + `[Produces]` | 见下方示例 |
| 无返回体 | `Task` 或 `IActionResult` | `public async Task Delete(...)` |
| 匿名类型 | `IActionResult` + `[ProducesResponseType]` | 见下方示例 |

### 特殊情况处理

**1. 文件下载**

```csharp
[HttpGet("{id:guid}/download")]
[Produces("application/octet-stream")]
public async Task<IActionResult> Download(Guid id) { ... }
```

**2. 匿名类型返回**

```csharp
[HttpGet("me")]
[ProducesResponseType(typeof(object), 200)]
public async Task<IActionResult> Me() { ... }
```

**3. CreatedAtAction（201 响应）**

```csharp
[HttpPost]
[ProducesResponseType(typeof(ProductDto), 201)]
public async Task<IActionResult> Create(CreateProductDto input) { ... }
```

**4. 无返回体的操作**

```csharp
// 推荐：直接返回 Task，Swagger 显示为 200 空响应
[HttpPut("{id:guid}")]
public async Task Update(Guid id, UpdateDto input) { ... }

// 也可以：返回 204 NoContent
[HttpDelete("{id:guid}")]
public async Task<IActionResult> Delete(Guid id) { ... }
```

### 验收检查

新增接口时检查：
- [ ] 有具体 DTO 返回的方法使用了 `ActionResult<T>`。
- [ ] 匿名类型或文件流使用了 `[ProducesResponseType]` 标注。
- [ ] Scalar 文档中能正确显示响应体结构。

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
- [ ] 返回类型使用 `ActionResult<T>`，Scalar 能显示响应体结构。
