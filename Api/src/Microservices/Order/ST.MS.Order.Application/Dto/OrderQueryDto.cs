using ST.MS.Order.Domain.Enums;
using ST.Shared.Application.Dtos;

namespace ST.MS.Order.Application.Dto;

/// <summary>
/// 订单列表查询条件。
/// </summary>
public sealed class OrderQueryDto : PagedRequestDto
{
	/// <summary>订单号关键字（模糊匹配）</summary>
	public string? OrderNo { get; set; }

	/// <summary>订单状态筛选</summary>
	public OrderStatus? Status { get; set; }
}
