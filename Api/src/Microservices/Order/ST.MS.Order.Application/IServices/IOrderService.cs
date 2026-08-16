using ST.MS.Order.Application.Dto;
using ST.Shared.Application;
using ST.Shared.Application.Dtos;

namespace ST.MS.Order.Application.IServices;

/// <summary>
/// 订单服务接口。
/// </summary>
public interface IOrderService : IAppService
{
	/// <summary>
	/// 创建订单（含 Outbox 消息写入，同一事务）。
	/// </summary>
	Task<OrderDto> CreateOrderAsync(CreateOrderDto input, CancellationToken ct = default);

	/// <summary>
	/// 查询订单详情。
	/// </summary>
	Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default);

	/// <summary>
	/// 订单列表分页查询。
	/// </summary>
	Task<PagedResultDto<OrderDto>> GetOrdersAsync(OrderQueryDto query, CancellationToken ct = default);

	/// <summary>
	/// 取消订单（含 Outbox 取消事件写入，同一事务）。
	/// </summary>
	Task<OrderDto> CancelOrderAsync(Guid orderId, string reason, CancellationToken ct = default);
}
