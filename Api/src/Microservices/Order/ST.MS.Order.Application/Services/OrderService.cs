using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Order.Application.Dto;
using ST.MS.Order.Domain.Entities;
using ST.MS.Order.Domain.Enums;
using ST.MS.Order.Infra.DbContext;
using ST.Shared.Application;
using ST.Shared.Exceptions;
using ST.Shared.Security;

using OrderEntity = ST.MS.Order.Domain.Entities.Order;

namespace ST.MS.Order.Application.Services;

/// <summary>
/// 订单服务实现。
/// 创建/取消订单时，业务数据与 Outbox 消息在同一事务中提交。
/// </summary>
public class OrderService : IOrderService, ITransientDependency
{
	private readonly OrderDbContext _dbContext;
	private readonly IOutboxStore _outboxStore;
	private readonly ICurrentTenantAccessor _tenantAccessor;
	private readonly ITenantQuotaService? _quotaService;
	private readonly ILogger<OrderService> _logger;

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public OrderService(
		OrderDbContext dbContext,
		IOutboxStore outboxStore,
		ICurrentTenantAccessor tenantAccessor,
		ILogger<OrderService> logger,
		ITenantQuotaService? quotaService = null)
	{
		_dbContext = dbContext;
		_outboxStore = outboxStore;
		_tenantAccessor = tenantAccessor;
		_quotaService = quotaService;
		_logger = logger;
	}

	public async Task<OrderDto> CreateOrderAsync(CreateOrderDto input, CancellationToken ct = default)
	{
		if (input.Items.Count == 0)
		{
			throw new BusinessException("订单至少包含一个商品");
		}

		// 租户配额检查
		if (_tenantAccessor.TenantId.HasValue && _quotaService is not null)
		{
			await _quotaService.CheckOrderQuotaAsync(_tenantAccessor.TenantId.Value, ct);
		}

		var sw = System.Diagnostics.Stopwatch.StartNew();

		// 生成订单号
		var orderNo = GenerateOrderNo();

		// 构建订单项
		var orderItems = input.Items.Select(item => new OrderItem(
			item.SkuId, item.ProductName, item.Quantity, item.UnitPrice)).ToList();

		var totalAmount = orderItems.Sum(i => i.Quantity * i.UnitPrice);

		// 创建订单
		var order = new OrderEntity(orderNo, input.UserId, totalAmount, orderItems);

		// 创建 Saga 实例
		var saga = new SagaInstance(order.Id, "OrderSaga", "OrderCreated");
		saga.Steps.Add(new SagaStep(saga.Id, "OrderCreated"));
		saga.Steps.Add(new SagaStep(saga.Id, "InventoryFreezing", "InventoryReleased"));
		saga.Steps.Add(new SagaStep(saga.Id, "Paying", "PaymentRefund"));

		order.SagaInstanceId = saga.Id;

		// 写入 Outbox 消息（与订单同一事务）
		var integrationEvent = new OrderCreatedIntegrationEvent(
			order.Id,
			order.UserId,
			order.TotalAmount,
			orderItems.Select(i => new OrderItemData(i.SkuId, i.ProductName, i.Quantity, i.UnitPrice)).ToList());

		var outboxMessage = new OutboxMessage
		{
			AggregateId = order.Id,
			EventType = nameof(OrderCreatedIntegrationEvent),
			Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonOptions),
			Status = OutboxStatus.Pending,
			OccurredAtUtc = DateTime.UtcNow
		};

		// 同一事务：订单 + Saga + Outbox
		_dbContext.Orders.Add(order);
		_dbContext.SagaInstances.Add(saga);
		_outboxStore.Add(outboxMessage);

		await _dbContext.SaveChangesAsync(ct);

		sw.Stop();
		OrderMetrics.OrderCreated.Add(1);
		OrderMetrics.CreateDurationMs.Record(sw.Elapsed.TotalMilliseconds);

		_logger.LogInformation(
			"Order created. OrderId={OrderId} OrderNo={OrderNo} TotalAmount={TotalAmount}",
			order.Id, order.OrderNo, order.TotalAmount);

		return MapToDto(order);
	}

	public async Task<OrderDto?> GetOrderAsync(Guid orderId, CancellationToken ct = default)
	{
		var order = await _dbContext.Orders
			.Include(o => o.Items)
			.FirstOrDefaultAsync(o => o.Id == orderId, ct);

		return order is null ? null : MapToDto(order);
	}

	public async Task<OrderDto> CancelOrderAsync(Guid orderId, string reason, CancellationToken ct = default)
	{
		var order = await _dbContext.Orders
			.Include(o => o.Items)
			.FirstOrDefaultAsync(o => o.Id == orderId, ct)
			?? throw new BusinessException("订单不存在", errorCode: "ORDER_NOT_FOUND");

		order.Cancel(reason);

		// 写入取消事件到 Outbox
		var cancelEvent = new OrderCanceledIntegrationEvent(order.Id, reason);
		var outboxMessage = new OutboxMessage
		{
			AggregateId = order.Id,
			EventType = nameof(OrderCanceledIntegrationEvent),
			Payload = JsonSerializer.Serialize(cancelEvent, cancelEvent.GetType(), JsonOptions),
			Status = OutboxStatus.Pending,
			OccurredAtUtc = DateTime.UtcNow
		};

		_outboxStore.Add(outboxMessage);

		// 更新 Saga 状态
		if (order.SagaInstanceId.HasValue)
		{
			var saga = await _dbContext.SagaInstances
				.Include(s => s.Steps)
				.FirstOrDefaultAsync(s => s.Id == order.SagaInstanceId.Value, ct);

			if (saga is not null)
			{
				saga.StartCompensation(reason);
			}
		}

		await _dbContext.SaveChangesAsync(ct);

		OrderMetrics.OrderCanceled.Add(1);

		_logger.LogInformation(
			"Order canceled. OrderId={OrderId} Reason={Reason}",
			order.Id, reason);

		return MapToDto(order);
	}

	private static string GenerateOrderNo()
	{
		return $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
	}

	private static OrderDto MapToDto(OrderEntity order)
	{
		return new OrderDto
		{
			Id = order.Id,
			OrderNo = order.OrderNo,
			UserId = order.UserId,
			TotalAmount = order.TotalAmount,
			Status = order.Status,
			CreateTime = order.CreateTime,
			CancelReason = order.CancelReason,
			Items = order.Items.Select(i => new OrderItemDto
			{
				SkuId = i.SkuId,
				ProductName = i.ProductName,
				Quantity = i.Quantity,
				UnitPrice = i.UnitPrice,
				Subtotal = i.Quantity * i.UnitPrice
			}).ToList()
		};
	}
}
