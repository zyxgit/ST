using System.Diagnostics;
using ST.Shared;

namespace ST.Infra.EventBus.Abstractions;

public abstract record IntegrationEvent
{
	public Guid Id { get; init; } = Guid.NewGuid();

	public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;

	/// <summary>关联 ID，用于跨服务消息链路追踪（从调用方透传或自动生成）</summary>
	public string? CorrelationId { get; init; }

	/// <summary>W3C TraceId，用于关联分布式链路（从 Activity.Current 自动提取）</summary>
	public string? TraceId { get; init; }

	/// <summary>租户 ID（从 TenantContext 自动提取，用于跨服务租户上下文传播）</summary>
	public Guid? TenantId { get; init; }

	[JsonIgnore]
	public virtual string EventName => GetType().FullName ?? GetType().Name;

	/// <summary>
	/// 创建时自动从当前 Activity 填充 TraceId 和 TenantId（若调用方未显式指定）。
	/// </summary>
	public IntegrationEvent()
	{
		var activity = Activity.Current;
		if (activity is not null)
		{
			TraceId ??= activity.TraceId.ToString();
			CorrelationId ??= activity.TraceId.ToString();
		}

		TenantId ??= TenantContext.CurrentTenantId;
	}
}

