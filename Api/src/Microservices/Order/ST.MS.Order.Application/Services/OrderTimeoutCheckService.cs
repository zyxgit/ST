using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ST.MS.Order.Application.IServices;
using ST.MS.Order.Application.Options;
using ST.MS.Order.Domain.Enums;
using ST.MS.Order.Infra.DbContext;

namespace ST.MS.Order.Application.Services;

/// <summary>
/// 订单超时自动取消后台服务。
/// 定期扫描超过支付时限的 Pending / InventoryFrozen 订单，调用取消流程。
/// 取消会通过 Outbox 发布 OrderCanceledIntegrationEvent，触发 Inventory 释放冻结库存。
/// </summary>
public sealed class OrderTimeoutCheckService : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IOptions<OrderTimeoutOptions> _options;
	private readonly ILogger<OrderTimeoutCheckService> _logger;

	public OrderTimeoutCheckService(
		IServiceScopeFactory scopeFactory,
		IOptions<OrderTimeoutOptions> options,
		ILogger<OrderTimeoutCheckService> logger)
	{
		_scopeFactory = scopeFactory;
		_options = options;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		if (!_options.Value.Enabled)
		{
			_logger.LogInformation("Order timeout check service is disabled.");
			return;
		}

		var interval = TimeSpan.FromSeconds(_options.Value.CheckIntervalSeconds);
		_logger.LogInformation(
			"Order timeout check service started. Interval={Interval} Timeout={Timeout}min BatchSize={BatchSize}",
			interval, _options.Value.PaymentTimeoutMinutes, _options.Value.BatchSize);

		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await CheckAndCancelTimeoutOrdersAsync(stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred during order timeout check.");
			}

			await Task.Delay(interval, stoppingToken);
		}
	}

	private async Task CheckAndCancelTimeoutOrdersAsync(CancellationToken ct)
	{
		using var scope = _scopeFactory.CreateScope();
		var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
		var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

		var timeoutThreshold = DateTime.UtcNow.AddMinutes(-_options.Value.PaymentTimeoutMinutes);
		var batchSize = _options.Value.BatchSize;

		// 查询超时订单：状态为 Pending 或 InventoryFrozen，且创建时间超过阈值
		var timeoutOrders = await dbContext.Orders
			.Where(o => (o.Status == OrderStatus.Pending || o.Status == OrderStatus.InventoryFrozen)
				&& o.CreateTime < timeoutThreshold)
			.OrderBy(o => o.CreateTime)
			.Take(batchSize)
			.Select(o => new { o.Id, o.OrderNo, o.Status, o.CreateTime })
			.ToListAsync(ct);

		if (timeoutOrders.Count == 0)
		{
			_logger.LogDebug("No timeout orders found.");
			return;
		}

		_logger.LogInformation("Found {Count} timeout orders to cancel.", timeoutOrders.Count);

		var successCount = 0;
		var failCount = 0;

		foreach (var orderInfo in timeoutOrders)
		{
			try
			{
				var elapsed = DateTime.UtcNow - orderInfo.CreateTime;
				var reason = $"支付超时自动取消（超时 {elapsed.TotalMinutes:F0} 分钟）";

				await orderService.CancelOrderAsync(orderInfo.Id, reason, ct);
				successCount++;

				_logger.LogInformation(
					"Timeout order canceled. OrderId={OrderId} OrderNo={OrderNo} Status={Status} Elapsed={Elapsed}min",
					orderInfo.Id, orderInfo.OrderNo, orderInfo.Status, elapsed.TotalMinutes);
			}
			catch (Exception ex)
			{
				failCount++;
				_logger.LogError(ex,
					"Failed to cancel timeout order. OrderId={OrderId} OrderNo={OrderNo}",
					orderInfo.Id, orderInfo.OrderNo);
			}
		}

		_logger.LogInformation(
			"Timeout order cancellation batch completed. Total={Total} Success={Success} Failed={Failed}",
			timeoutOrders.Count, successCount, failCount);
	}
}
