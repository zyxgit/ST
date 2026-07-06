using ST.Shared.Application;

namespace ST.MS.OperationLog.Application.IServices;

/// <summary>
/// 死信服务接口。
/// </summary>
public interface IDeadLetterService : IAppService
{
	/// <summary>
	/// 重放单条死信消息。
	/// </summary>
	Task<bool> ReplayAsync(Guid id);

	/// <summary>
	/// 批量重放死信消息。
	/// </summary>
	Task<(int Replayed, int Failed)> BatchReplayAsync(List<Guid> ids);
}
