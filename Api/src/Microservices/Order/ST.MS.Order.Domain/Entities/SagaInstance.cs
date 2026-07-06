using ST.MS.Order.Domain.Enums;

namespace ST.MS.Order.Domain.Entities;

/// <summary>
/// Saga 实例聚合根。
/// 记录跨服务事务的编排状态。
/// </summary>
public class SagaInstance : AggregateRoot
{
	/// <summary>关联的业务 ID（如 OrderId）</summary>
	public Guid BusinessId { get; set; }

	/// <summary>Saga 类型标识（如 "OrderSaga"）</summary>
	public string SagaType { get; set; } = string.Empty;

	/// <summary>当前步骤名称</summary>
	public string CurrentStep { get; set; } = string.Empty;

	/// <summary>Saga 状态</summary>
	public SagaStatus Status { get; set; } = SagaStatus.Started;

	/// <summary>已重试次数</summary>
	public int RetryCount { get; set; }

	/// <summary>最后一次错误信息</summary>
	public string? LastError { get; set; }

	/// <summary>Saga 步骤列表</summary>
	public List<SagaStep> Steps { get; set; } = [];

	public SagaInstance()
	{
	}

	public SagaInstance(Guid businessId, string sagaType, string firstStep)
	{
		Id = Guid.CreateVersion7();
		BusinessId = businessId;
		SagaType = sagaType;
		CurrentStep = firstStep;
		Status = SagaStatus.Started;
	}

	/// <summary>
	/// 推进到下一步骤。
	/// </summary>
	public void AdvanceTo(string nextStep)
	{
		CurrentStep = nextStep;
		Status = SagaStatus.Running;
	}

	/// <summary>
	/// 标记 Saga 已完成。
	/// </summary>
	public void Complete()
	{
		Status = SagaStatus.Completed;
	}

	/// <summary>
	/// 标记 Saga 进入补偿流程。
	/// </summary>
	public void StartCompensation(string reason)
	{
		Status = SagaStatus.Compensating;
		LastError = reason;
	}

	/// <summary>
	/// 标记 Saga 补偿完成。
	/// </summary>
	public void Compensate()
	{
		Status = SagaStatus.Compensated;
	}

	/// <summary>
	/// 标记 Saga 失败。
	/// </summary>
	public void Fail(string error)
	{
		Status = SagaStatus.Failed;
		LastError = error;
	}
}
