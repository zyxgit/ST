using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST.Infra.Redis.Inventory;
using ST.MS.Inventory.Application.Dto;
using ST.MS.Inventory.Application.Services;
using ST.MS.Inventory.Infra.DbContext;
using ST.Shared.WebApi.Controller;

namespace ST.MS.Inventory.Api.Controllers;

/// <summary>
/// 库存管理接口。
/// </summary>
[Route("api/inventory/skus")]
public class InventoryController : AbstractControllerBase
{
	private readonly IInventoryService _inventoryService;
	private readonly ILogger<InventoryController> _logger;

	private readonly InventoryDbContext _dbContext;
	private readonly IInventoryRedisService _inventoryRedis;

	public InventoryController(IInventoryService inventoryService, InventoryDbContext dbContext, IInventoryRedisService inventoryRedis, ILogger<InventoryController> logger)
	{
		_inventoryService = inventoryService;
		_dbContext = dbContext;
		_inventoryRedis = inventoryRedis;
		_logger = logger;
	}

	/// <summary>
	/// SKU 列表
	/// </summary>
	[HttpGet]
	[Authorize(Policy = "perm:inventory:sku:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult<List<SkuDto>>> GetSkus(CancellationToken ct)
	{
		var skus = await _inventoryService.GetSkusAsync(ct);
		return Ok(skus);
	}

	/// <summary>
	/// 创建 SKU
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "perm:inventory:sku:create", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult<SkuDto>> CreateSku([FromBody] CreateSkuDto input, CancellationToken ct)
	{
		var sku = await _inventoryService.CreateSkuAsync(input, ct);
		return CreatedAtAction(nameof(GetStock), new { skuId = sku.SkuId }, sku);
	}

	/// <summary>
	/// 增加库存
	/// </summary>
	[HttpPost("{skuId:guid}/stock/increase")]
	[Authorize(Policy = "perm:inventory:sku:stock", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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
	/// 扣减库存
	/// </summary>
	[HttpPost("{skuId:guid}/stock/deduct")]
	[Authorize(Policy = "perm:inventory:sku:stock", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult<SkuDto>> DeductStock(Guid skuId, [FromQuery] int quantity, CancellationToken ct)
	{
		if (quantity <= 0)
		{
			return BadRequest(new { Error = "数量必须大于 0" });
		}

		var sku = await _inventoryService.DeductStockAsync(skuId, quantity, ct);
		return Ok(sku);
	}

	/// <summary>
	/// 查询库存
	/// </summary>
	[HttpGet("{skuId:guid}/stock")]
	[Authorize(Policy = "perm:inventory:sku:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult<SkuDto>> GetStock(Guid skuId, CancellationToken ct)
	{
		var sku = await _inventoryService.GetSkuAsync(skuId, ct);

		if (sku is null)
		{
			return NotFound(new { Error = "SKU 不存在", SkuId = skuId });
		}

		return Ok(sku);
	}

	/// <summary>
	/// 诊断：同时查 DB 和 Redis 库存，对比数据一致性
	/// </summary>
	[HttpGet("debug/db-stock")]
	[Authorize(Policy = "perm:inventory:sku:query", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<ActionResult> GetDbStock(CancellationToken ct)
	{
		var skus = await _dbContext.Skus
			.AsNoTracking()
			.ToListAsync(ct);

		var freezeRecords = await _dbContext.FreezeRecords
			.AsNoTracking()
			.Select(r => new { r.OrderId, r.SkuId, r.Quantity, Status = r.Status.ToString() })
			.ToListAsync(ct);

		// 同时查 Redis
		var redisResults = new List<object>();
		foreach (var sku in skus)
		{
			var redis = await _inventoryRedis.GetStockAsync(sku.SkuId, ct);
			redisResults.Add(new
			{
				sku.SkuId,
				sku.ProductName,
				Db = new { sku.Available, sku.Frozen, sku.Sold },
				Redis = redis.HasValue
					? new { redis.Value.Available, redis.Value.Frozen, redis.Value.Sold }
					: null
			});
		}

		return Ok(new { StockComparison = redisResults, FreezeRecords = freezeRecords });
	}
}
