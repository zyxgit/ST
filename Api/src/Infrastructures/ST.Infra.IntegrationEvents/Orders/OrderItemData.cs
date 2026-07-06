namespace ST.Infra.IntegrationEvents.Orders;

/// <summary>
/// 订单项数据，用于跨服务传递订单项信息。
/// </summary>
/// <param name="SkuId">SKU ID</param>
/// <param name="ProductName">商品名称</param>
/// <param name="Quantity">数量</param>
/// <param name="UnitPrice">单价</param>
public sealed record OrderItemData(
	Guid SkuId,
	string ProductName,
	int Quantity,
	decimal UnitPrice);
