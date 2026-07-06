using ST.Shared.Application.Dtos;

namespace ST.MS.Identity.Application.Dtos.Tenant;

/// <summary>
/// 租户查询条件
/// </summary>
public sealed class TenantQueryInputDto : PagedRequestDto
{
	/// <summary>
	/// 关键字（模糊匹配编码/名称）
	/// </summary>
	public string? Keyword { get; set; }

	/// <summary>
	/// 状态过滤
	/// </summary>
	public string? Status { get; set; }
}
