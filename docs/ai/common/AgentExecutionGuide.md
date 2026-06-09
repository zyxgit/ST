# AI Agent 路线图执行指南

本文回答一个核心问题：**如何让 Codex、Claude Code 或其他 AI 编码助手按照 `DevelopmentRoadmap.md` 持续完成剩余开发，而不是一次性生成失控的大改动。**

结论：不要把“完成第一阶段”这种大目标直接丢给 AI。应该采用 **路线图 → Epic → 小任务 → 单次 Prompt → 验收 → 合并 → 下一任务** 的节奏。

## 推荐工作流

```text
1. 选择路线图阶段
2. 拆成 Epic / Issue
3. 每次只给 AI 一个可交付小任务
4. 在 Prompt 中固定读哪些文档、改哪些范围、不能改哪些范围
5. 要求 AI 先给实施计划，再编码
6. 编码后跑测试并同步文档
7. 人类 Review diff
8. 通过后合并，再进入下一个小任务
```

## 为什么不能直接让 AI 完成整个阶段

`DevelopmentRoadmap.md` 中的阶段是产品和架构级目标，不是单次编码任务。例如“订单 Saga 与可靠消息样板”同时涉及：

- 新微服务。
- 数据库实体与迁移。
- RabbitMQ 事件。
- Outbox / Inbox 基础设施。
- Gateway 路由。
- Aspire 和 Docker Compose 编排。
- 压测脚本。
- 文档同步。

如果一次性要求 AI 完成全部内容，很容易出现：

- 生成大量不可编译代码。
- 路径、命名空间、项目引用不一致。
- 文档与代码不同步。
- 忽略迁移、网关、部署、测试等收尾工作。
- 跨服务边界混乱。

因此必须把路线图拆成小块，并让每个小块都能独立 review。

## Codex / Claude Code 的项目入口

| 工具 | 建议入口 | 用途 |
|------|----------|------|
| Codex / 通用 Agent | `AGENTS.md` | 仓库级强制说明，提示必须阅读路线图与执行指南 |
| Claude Code | `.claude/CLAUDE.md` | Claude Code 项目记忆入口，保持与 `docs/ai/**` 一致 |
| Copilot | `.github/copilot-instructions.md` | GitHub Copilot 项目指令 |
| 所有 Agent | `docs/ai/AI-RULES.md` | ST 项目 AI 规则统一入口 |
| 所有 Agent | `docs/ai/common/DevelopmentRoadmap.md` | 分阶段路线图 |
| 所有 Agent | `docs/ai/common/DocumentationSync.md` | 功能变更时必须同步哪些文档 |

## 标准 Prompt 模板

后续给 Codex / Claude Code 派任务时，建议始终使用下面模板。

```text
你在 ST 仓库中工作。请先阅读：
- AGENTS.md
- docs/ai/AI-RULES.md
- docs/ai/common/AgentExecutionGuide.md
- docs/ai/common/DevelopmentRoadmap.md
- docs/ai/common/DocumentationSync.md
- 与本任务相关的 docs/ai/api 或 docs/ai/web 专题

目标：
[用 1-3 句话描述本次小任务，不要写“完成整个阶段”]

范围：
- 允许修改：[列出目录 / 项目 / 文件]
- 不允许修改：[列出暂不触碰的目录 / 功能]

验收标准：
- [可编译 / 可运行]
- [API 或业务链路能验证]
- [文档已同步]
- [测试或命令]

执行要求：
1. 先用 rg / 现有文件确认项目结构和同类实现。
2. 先输出简短实施计划。
3. 再修改代码。
4. 修改后运行必要检查。
5. 最终说明变更文件、测试命令、未完成事项。
```

## 第一阶段推荐拆票方式

第一阶段不要直接说“完成订单 Saga”。建议拆成 8 个任务，每个任务单独给 AI。

### Task 1：可靠消息表与抽象

目标：只实现 Outbox / Inbox 的基础模型、DbContext 配置、接口和文档，不接业务服务。

建议 Prompt：

```text
请基于 DevelopmentRoadmap 第一阶段，先实现可靠消息基础设施的第一小步：Outbox / Inbox 表模型与抽象。

允许修改：
- Api/src/Infrastructures/ST.Infra.EventBus/** 或新建 ST.Infra.ReliableMessaging 项目
- docs/ai/common/DevelopmentRoadmap.md
- docs/ai/api/README.md
- docs/database/README.md

不允许修改：
- Identity / FileUpload / OperationLog 业务逻辑
- Gateway 路由

验收标准：
- 新增 OutboxMessage、InboxMessage 模型或等价结构。
- 提供发布状态、重试次数、下一次重试时间、错误信息字段。
- 提供消费幂等所需的 MessageId + Consumer 约束设计。
- dotnet build Api/src/ST.slnx 通过，若因环境限制不能运行需说明。
- 文档说明表结构和使用边界。
```

### Task 2：Outbox Publisher 后台任务

目标：扫描待发送 Outbox 消息并通过现有 RabbitMQ EventBus 投递。

验收重点：

- 支持批量扫描。
- 支持重试和 `next_retry_at_utc`。
- 投递成功后标记已发送。
- 投递失败记录错误。
- 不引入业务服务依赖。

### Task 3：Order Service 骨架

目标：只建立 Order 微服务骨架、实体、DbContext、Controller、基础创建订单 API。

验收重点：

- 项目加入 `Api/src/ST.slnx`。
- 使用现有 `AddSharedWebApi` / `UseSharedWebApi` 风格。
- 创建订单只写本地订单，不做库存和支付。
- Gateway / Aspire / Docker Compose 可先不接，或在本任务明确接入。

### Task 4：Inventory Service 骨架与库存冻结

目标：建立库存服务并提供库存初始化、查询、冻结、释放接口。

验收重点：

- 数据库条件更新或乐观锁防超卖。
- 预留 Redis Lua，但可以作为下一任务实现。
- 重复冻结同一订单必须幂等。

### Task 5：Redis Lua 库存预扣

目标：把 Inventory 冻结库存升级为 Redis Lua 原子预扣 + 数据库兜底。

验收重点：

- 明确 Redis 键空间。
- Lua 脚本原子判断库存是否足够。
- Redis 成功但 DB 失败时有补偿或回滚策略。
- 提供并发验证脚本。

### Task 6：Payment Mock Service

目标：新增模拟支付服务，支持支付成功、支付失败、支付超时事件。

验收重点：

- 不接真实三方支付。
- 事件通过 Outbox 可靠发布。
- API 可手动触发成功 / 失败。

### Task 7：Saga 流程串联

目标：串联 Order、Inventory、Payment，形成完整下单流程。

验收重点：

- OrderCreated → InventoryFrozen → PaymentSucceeded → OrderPaid。
- PaymentFailed / Timeout → OrderCanceled → InventoryReleased。
- 消息重复投递时状态不乱。
- Saga 状态表可查询当前步骤。

### Task 8：压测、可观测性和文档收尾

目标：补齐第一阶段的可证明材料。

验收重点：

- k6 或等价压测脚本。
- README / docs 更新。
- Grafana 或日志查询说明。
- 最终模板能力说明更新。

## 第二阶段推荐拆票方式

### Task 1：Gateway Redis 限流抽象

- 新增限流配置模型。
- 新增 Redis 计数服务或 Lua 执行服务。
- 暂不替换现有 FixedWindow 限流。

### Task 2：Gateway 接入分布式限流

- 按 IP / 用户 / Path 分区。
- 保留配置开关：`Mode = InMemory | Redis`。
- 登录、验证码、上传接口支持独立规则。

### Task 3：权限缓存

- Identity 登录或权限查询时缓存权限集合。
- 角色、菜单、权限变更时失效缓存。
- 文档说明 Redis 键空间。

## 第三阶段推荐拆票方式

### Task 1：分片上传会话模型

- `file_upload_sessions`。
- `file_upload_chunks`。
- init/status API。

### Task 2：分片上传与幂等

- chunk upload API。
- Redis Bitmap / Set 记录已上传分片。
- 重复分片幂等。

### Task 3：异步合并与秒传

- complete API。
- 后台合并任务。
- 文件 Hash 秒传。

### Task 4：签名下载 URL

- 私有文件短期签名 URL。
- 过期校验。
- 用户或租户维度预留。

## 每次任务的 Review 清单

人类 Review AI 生成的 PR 时，至少检查：

- 是否只完成本次小任务，没有偷偷扩大范围。
- 是否遵循现有命名空间、项目结构、启动方式。
- 是否新增真实可运行代码，而不是空壳。
- 是否更新相关文档。
- 是否有测试命令或环境限制说明。
- 是否没有提交密钥、连接串、本地配置。
- 是否能回滚：一个 PR 只解决一个主题。

## 推荐使用方式

### 给 Codex

1. 确保仓库根目录有 `AGENTS.md`。
2. 新开一个分支。
3. 一次只复制一个 Task Prompt。
4. 要求 Codex 先计划，再实现。
5. 让 Codex 跑测试并提交。
6. 人类 review 后再继续下一 Task。

### 给 Claude Code

1. 确保 `.claude/CLAUDE.md` 已指向本指南和路线图。
2. 开始会话后先让 Claude 总结本任务涉及的现有代码路径。
3. 使用上面的标准 Prompt 模板。
4. 如果 Claude 开始扩大范围，立即要求它停止并回到当前 Task 验收标准。

### 推荐第一条实际开发指令

如果要开始写第一阶段，建议第一条指令不要超过这个范围：

```text
请只完成 DevelopmentRoadmap 第一阶段 Task 1：可靠消息表与抽象。
不要创建 Order / Inventory / Payment 服务，不要改 Gateway。
先阅读 AGENTS.md、docs/ai/AI-RULES.md、docs/ai/common/AgentExecutionGuide.md、docs/ai/common/DevelopmentRoadmap.md、docs/ai/common/DocumentationSync.md。
完成后运行 dotnet build Api/src/ST.slnx，并同步更新相关文档。
```
