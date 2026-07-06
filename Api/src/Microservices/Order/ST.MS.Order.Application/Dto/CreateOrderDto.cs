namespace ST.MS.Order.Application.Dto;

/// <summary>
/// 创建订单请求。
/// </summary>
public sealed class CreateOrderDto
{
	/// <summary>下单用户 ID</summary>
	public Guid UserId { get; set; }

	/// <summary>订单项列表</summary>
	public List<CreateOrderItemDto> Items { get; set; } = [];
}
