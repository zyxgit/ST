using System.Text.Json;
using Microsoft.Extensions.Logging;
using ST.Infra.EventBus.Abstractions;
using ST.Infra.IntegrationEvents.Orders;
using ST.Infra.ReliableMessaging.Abstractions;
using ST.Infra.ReliableMessaging.Models;
using ST.MS.Payment.Infra.DbContext;

namespace ST.MS.Payment.Application.Services;

/// <summary>
/// 处理 OrderCreatedIntegrationEvent。
/// 创建待支付记录，等待手动触发支付（模拟）。
/// </summary>
public class OrderCreatedHandler : IIntegrationEventHandler<OrderCreatedIntegrationEvent>, ITransientDependency
{
	private readonly PaymentDbContext _dbContext;
	private readonly IInboxStore _inboxStore;
	private readonly ILogger<OrderCreatedHandler> _logger;

	private const string Consumer = "PaymentService";

	public OrderCreatedHandler(
		PaymentDbContext dbContext,
		IInboxStore inboxStore,
		ILogger<OrderCreatedHandler> logger)
	{
		_dbContext = dbContext;
		_inboxStore = inboxStore;
		_logger = logger;
	}

	public async Task HandleAsync(OrderCreatedIntegrationEvent @event, CancellationToken cancellationToken = default)
	{
		// 幂等检查
		if (await _inboxStore.ExistsAsync(@event.Id, Consumer, cancellationToken))
		{
			_logger.LogDebug("OrderCreated event already processed. EventId={EventId}", @event.Id);
			return;
		}

		// 记录 Inbox
		_inboxStore.Add(new InboxMessage
		{
			MessageId = @event.Id,
			Consumer = Consumer,
			EventType = nameof(OrderCreatedIntegrationEvent),
			ReceivedAtUtc = DateTime.UtcNow
		});

		// 创建待支付记录
		var payment = new Domain.Entities.Payment(@event.OrderId, @event.OrderNo, @event.TotalAmount);
		_dbContext.Payments.Add(payment);

		await _inboxStore.MarkAsProcessedAsync(@event.Id, Consumer, cancellationToken);
		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation(
			"Pending payment created for OrderId={OrderId} Amount={Amount} PaymentId={PaymentId}",
			@event.OrderId, @event.TotalAmount, payment.Id);
	}
}
