using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.MS.Order.Application.Dto;
using ST.MS.Order.Application.Services;
using ST.Shared.Application.Dtos;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Order.Api.Controllers;

/// <summary>
/// 订单管理接口。
/// </summary>
public class OrdersController : AbstractControllerBase
{
	private readonly IOrderService _orderService;
	private readonly ILogger<OrdersController> _logger;

	public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
	{
		_orderService = orderService;
		_logger = logger;
	}

	/// <summary>
	/// 创建订单
	/// </summary>
	/// <param name="input">订单信息</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>创建的订单</returns>
	[HttpPost]
	[Authorize(Policy = "perm:order:list:create", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto input, CancellationToken ct)
	{
		var order = await _orderService.CreateOrderAsync(input, ct);

		_logger.LogInformation("Order created via API. OrderId={OrderId}", order.Id);

		return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
	}

	/// <summary>
	/// 订单列表
	/// </summary>
	[HttpGet]
	[Authorize(Policy = "perm:order:list:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult<PagedResultDto<OrderDto>>> GetOrders([FromQuery] OrderQueryDto query, CancellationToken ct)
	{
		var result = await _orderService.GetOrdersAsync(query, ct);
		return Ok(result);
	}

	/// <summary>
	/// 查询订单详情
	/// </summary>
	/// <param name="id">订单 ID</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>订单详情</returns>
	[HttpGet("{id:guid}")]
	[Authorize(Policy = "perm:order:list:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult<OrderDto>> GetOrder(Guid id, CancellationToken ct)
	{
		var order = await _orderService.GetOrderAsync(id, ct);

		if (order is null)
		{
			return NotFound(new { Error = "订单不存在", OrderId = id });
		}

		return Ok(order);
	}

	/// <summary>
	/// 取消订单
	/// </summary>
	/// <param name="id">订单 ID</param>
	/// <param name="input">取消原因</param>
	/// <param name="ct">取消令牌</param>
	/// <returns>取消后的订单</returns>
	[HttpPost("{id:guid}/cancel")]
	[Authorize(Policy = "perm:order:list:cancel", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult<OrderDto>> CancelOrder(Guid id, [FromBody] CancelOrderDto input, CancellationToken ct)
	{
		var order = await _orderService.CancelOrderAsync(id, input.Reason, ct);

		_logger.LogInformation("Order canceled via API. OrderId={OrderId} Reason={Reason}", id, input.Reason);

		return Ok(order);
	}
}

/// <summary>
/// 取消订单请求。
/// </summary>
public sealed class CancelOrderDto
{
	/// <summary>取消原因</summary>
	public string Reason { get; set; } = "用户取消";
}
