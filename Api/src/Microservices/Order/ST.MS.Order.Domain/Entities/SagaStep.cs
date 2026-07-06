namespace ST.MS.Order.Domain.Entities;

/// <summary>
/// Saga 步骤实体。
/// </summary>
public class SagaStep : Entity
{
	/// <summary>所属 Saga 实例 ID</summary>
	public Guid SagaId { get; set; }

	/// <summary>步骤名称（如 "InventoryFreezing", "Paying"）</summary>
	public string StepName { get; set; } = string.Empty;

	/// <summary>步骤状态（Pending/Completed/Failed/Compensated）</summary>
	public string Status { get; set; } = "Pending";

	/// <summary>请求负载（JSON）</summary>
	public string? RequestJson { get; set; }

	/// <summary>响应负载（JSON）</summary>
	public string? ResponseJson { get; set; }

	/// <summary>补偿事件类型（用于 Saga 回滚）</summary>
	public string? CompensationEvent { get; set; }

	public SagaStep()
	{
	}

	public SagaStep(Guid sagaId, string stepName, string? compensationEvent = null)
	{
		Id = Guid.CreateVersion7();
		SagaId = sagaId;
		StepName = stepName;
		CompensationEvent = compensationEvent;
	}
}
