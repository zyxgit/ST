using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ST.MS.Inventory.Application.Dto;
using ST.MS.Inventory.Application.Services;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Inventory.Api.Controllers;

/// <summary>
/// 库存管理接口。
/// </summary>
[AllowAnonymous]
public class InventoryController : AbstractControllerBase
{
	private readonly IInventoryService _inventoryService;
	private readonly ILogger<InventoryController> _logger;

	public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
	{
		_inventoryService = inventoryService;
		_logger = logger;
	}

	/// <summary>
	/// SKU 列表
	/// </summary>
	[HttpGet("api/inventory/skus")]
	public async Task<ActionResult<List<SkuDto>>> GetSkus(CancellationToken ct)
	{
		var skus = await _inventoryService.GetSkusAsync(ct);
		return Ok(skus);
	}

	/// <summary>
	/// 创建 SKU
	/// </summary>
	[HttpPost("api/inventory/skus")]
	public async Task<ActionResult<SkuDto>> CreateSku([FromBody] CreateSkuDto input, CancellationToken ct)
	{
		var sku = await _inventoryService.CreateSkuAsync(input, ct);
		return CreatedAtAction(nameof(GetStock), new { skuId = sku.SkuId }, sku);
	}

	/// <summary>
	/// 增加库存
	/// </summary>
	[HttpPost("api/inventory/skus/{skuId:guid}/stock/increase")]
	public async Task<ActionResult<SkuDto>> IncreaseStock(Guid skuId, [FromQuery] int quantity, CancellationToken ct)
	{
		if (quantity <= 0)
		{
			return BadRequest(new { Error = "数量必须大于 0" });
		}

		var sku = await _inventoryService.IncreaseStockAsync(skuId, quantity, ct);
		return Ok(sku);
	}

	/// <summary>
	/// 查询库存
	/// </summary>
	[HttpGet("api/inventory/skus/{skuId:guid}/stock")]
	public async Task<ActionResult<SkuDto>> GetStock(Guid skuId, CancellationToken ct)
	{
		var sku = await _inventoryService.GetSkuAsync(skuId, ct);

		if (sku is null)
		{
			return NotFound(new { Error = "SKU 不存在", SkuId = skuId });
		}

		return Ok(sku);
	}
}
