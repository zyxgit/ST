using ST.MS.Inventory.Domain.Enums;

namespace ST.MS.Inventory.Domain.Entities;

/// <summary>
/// 库存冻结记录。
/// 记录每个订单冻结了哪些 SKU 的多少库存，用于释放或转售。
/// </summary>
public class InventoryFreezeRecord : Entity
{
	/// <summary>关联的订单 ID</summary>
	public Guid OrderId { get; set; }

	/// <summary>SKU ID</summary>
	public Guid SkuId { get; set; }

	/// <summary>冻结数量</summary>
	public int Quantity { get; set; }

	/// <summary>冻结状态</summary>
	public FreezeStatus Status { get; set; } = FreezeStatus.Frozen;

	public InventoryFreezeRecord()
	{
	}

	public InventoryFreezeRecord(Guid orderId, Guid skuId, int quantity)
	{
		Id = Guid.CreateVersion7();
		OrderId = orderId;
		SkuId = skuId;
		Quantity = quantity;
		Status = FreezeStatus.Frozen;
	}

	/// <summary>标记已释放。</summary>
	public void MarkReleased()
	{
		Status = FreezeStatus.Released;
	}

	/// <summary>标记已售。</summary>
	public void MarkSold()
	{
		Status = FreezeStatus.Sold;
	}
}
