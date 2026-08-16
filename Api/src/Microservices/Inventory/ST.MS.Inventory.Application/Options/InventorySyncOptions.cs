namespace ST.MS.Inventory.Application.Options;

/// <summary>
/// 库存 Redis 同步配置。
/// </summary>
public sealed class InventorySyncOptions
{
	/// <summary>配置节名称</summary>
	public const string SectionName = "InventorySync";

	/// <summary>是否启用定时同步，默认启用</summary>
	public bool Enabled { get; set; } = true;

	/// <summary>同步间隔（秒），默认 300（5 分钟）</summary>
	public int SyncIntervalSeconds { get; set; } = 300;
}
