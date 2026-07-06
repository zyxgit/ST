namespace ST.MS.Order.Domain.Enums;

/// <summary>
/// Saga 实例状态。
/// </summary>
public enum SagaStatus
{
	/// <summary>已启动</summary>
	Started = 0,

	/// <summary>执行中</summary>
	Running = 1,

	/// <summary>已完成</summary>
	Completed = 2,

	/// <summary>补偿中</summary>
	Compensating = 3,

	/// <summary>已补偿（取消）</summary>
	Compensated = 4,

	/// <summary>失败</summary>
	Failed = 5
}
