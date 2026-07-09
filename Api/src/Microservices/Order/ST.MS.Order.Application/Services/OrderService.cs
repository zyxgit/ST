using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.Redis.Inventory;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Order.Application.Dto;
using ST.MS.Order.Domain.Entities;
using ST.Shared.Application.Dtos;
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
	private readonly IInventoryRedisService _inventoryRedis;
	private readonly ICurrentTenantAccessor _tenantAccessor;
	private readonly ITenantQuotaService? _quotaService;
	private readonly ILogger<OrderService> _logger;

	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

	public OrderService(
		OrderDbContext dbContext,
		IOutboxStore outboxStore,
		IInventoryRedisService inventoryRedis,
		ICurrentTenantAccessor tenantAccessor,
		ILogger<OrderService> logger,
		ITenantQuotaService? quotaService = null)
	{
		_dbContext = dbContext;
		_outboxStore = outboxStore;
		_inventoryRedis = inventoryRedis;
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

		// ── 同步库存预扣（Redis Lua 原子操作，check-and-decrement 一步完成） ──
		// 解决并发竞态：多个请求同时通过只读检查导致超卖
		var frozenItems = new List<(Guid SkuId, int Quantity)>();
		try
		{
			foreach (var item in input.Items)
			{
				var frozen = await _inventoryRedis.TryFreezeAsync(item.SkuId, item.Quantity, ct);
				if (!frozen)
				{
					// 库存不足，回滚已预扣的项
					foreach (var prev in frozenItems)
					{
						await _inventoryRedis.ReleaseAsync(prev.SkuId, prev.Quantity, ct);
					}

					throw new BusinessException(
						$"商品「{item.ProductName}」库存不足",
						errorCode: "INSUFFICIENT_STOCK");
				}

				frozenItems.Add((item.SkuId, item.Quantity));
			}
		}
		catch (BusinessException)
		{
			throw;
		}
		catch (Exception ex)
		{
			// 非业务异常也要回滚 Redis 预扣
			foreach (var prev in frozenItems)
			{
				await _inventoryRedis.ReleaseAsync(prev.SkuId, prev.Quantity, ct);
			}

			_logger.LogError(ex, "Redis pre-freeze failed unexpectedly.");
			throw;
		}

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
		// RedisPreFrozen=true 告知 Inventory 服务跳过 Redis 预扣，仅做 DB 兜底
		var integrationEvent = new OrderCreatedIntegrationEvent(
			order.Id,
			order.UserId,
			order.TotalAmount,
			orderItems.Select(i => new OrderItemData(i.SkuId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
			RedisPreFrozen: true);

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

		try
		{
			await _dbContext.SaveChangesAsync(ct);
		}
		catch
		{
			// DB 保存失败，回滚 Redis 预扣
			foreach (var prev in frozenItems)
			{
				await _inventoryRedis.ReleaseAsync(prev.SkuId, prev.Quantity, ct);
			}

			throw;
		}

		OrderMetrics.OrderCreated.Add(1);

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

	public async Task<PagedResultDto<OrderDto>> GetOrdersAsync(OrderQueryDto query, CancellationToken ct = default)
	{
		var (pageIndex, pageSize, skip) = query.Normalize();

		var ordersQuery = _dbContext.Orders
			.Include(o => o.Items)
			.AsNoTracking();

		if (!string.IsNullOrWhiteSpace(query.OrderNo))
		{
			ordersQuery = ordersQuery.Where(o => o.OrderNo.Contains(query.OrderNo));
		}

		if (query.Status.HasValue)
		{
			ordersQuery = ordersQuery.Where(o => o.Status == query.Status.Value);
		}

		var totalCount = await ordersQuery.LongCountAsync(ct);

		var orders = await ordersQuery
			.OrderByDescending(o => o.CreateTime)
			.Skip(skip)
			.Take(pageSize)
			.ToListAsync(ct);

		return new PagedResultDto<OrderDto>
		{
			PageIndex = pageIndex,
			PageSize = pageSize,
			TotalCount = totalCount,
			Items = orders.Select(MapToDto).ToList()
		};
	}

	public async Task<OrderDto> CancelOrderAsync(Guid orderId, string reason, CancellationToken ct = default)
	{
		var order = await _dbContext.Orders
			.Include(o => o.Items)
			.FirstOrDefaultAsync(o => o.Id == orderId, ct)
			?? throw new BusinessException("订单不存在", errorCode: "ORDER_NOT_FOUND");

		// Pending 状态说明 Redis 预扣已完成但 DB 冻结可能未执行，立即释放 Redis 预扣
		if (order.Status == OrderStatus.Pending)
		{
			foreach (var item in order.Items)
			{
				await _inventoryRedis.ReleaseAsync(item.SkuId, item.Quantity, ct);
			}
		}

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
