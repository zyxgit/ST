# Hangfire 与后台任务

## 目录

- [事实](#事实)
- [调度抽象](#调度抽象)
- [代码示例](#代码示例)
- [推荐方案](#推荐方案)
- [禁止事项](#禁止事项)
- [AI 注意事项](#ai-注意事项)

## 事实

- 程序集：`ST.Infra.Tasks`
- 扩展：`AddInfraTasks()` 注册 **`IBackgroundTaskScheduler`** 的实现 **`HangfireBackgroundTaskScheduler`**。
- 即刻执行任务另有 **`ImmediateTaskExecutor`** / **`PersistentTaskExecutor`**（按现有用法）。

注册摘录：

```csharp
services.AddSingleton<IBackgroundTaskScheduler, HangfireBackgroundTaskScheduler>();
```

文件：`Api/src/Infrastructures/ST.Infra.Tasks/Extensions/ServiceCollectionExtensions.cs`

## 调度抽象

- 通过 **`IBackgroundTaskScheduler`** 调度持久任务，避免控制器直接依赖 Hangfire API（除非维护 Hangfire 仪表盘或高级特性）。

## 代码示例

接口定义（真实源码 `ST.Infra.Tasks/Abstractions/IBackgroundTaskScheduler.cs`）：

```csharp
public interface IBackgroundTaskScheduler
{
	string Enqueue(Func<CancellationToken, Task> job);
	string Schedule(Func<CancellationToken, Task> job, TimeSpan delay);
	string Recurring(string jobId, Func<CancellationToken, Task> job, string cron);
	void Remove(string jobId);
}
```

应用服务中注入 `IBackgroundTaskScheduler` 后延时执行（示例委托签名与实现一致即可）：

```csharp
_scheduler.Schedule(_ => Task.CompletedTask, TimeSpan.FromMinutes(5));
```

## 推荐方案

- 长任务：设置重试、超时、**幂等**（至少一次投递语义下防重复写）。
- 多实例部署时确保 Hangfire 存储在 **共享 SQL/Redis**（由运维配置，不在此展开）。

## 禁止事项

- 禁止在 Hangfire 作业中直接使用 **请求 Scope 外** 的 `DbContext`（应通过 `IServiceScopeFactory` 创建作用域）。

## AI 注意事项

- 添加新定时任务前搜索 **`HangfireBackgroundTaskScheduler`** 用法，保持 DI 与队列命名一致。
