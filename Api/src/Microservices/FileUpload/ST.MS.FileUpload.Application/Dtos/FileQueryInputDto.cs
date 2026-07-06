using ST.Shared.Application.Dtos;

namespace ST.MS.FileUpload.Application.Dtos;

/// <summary>
/// 文件列表分页查询条件。
/// </summary>
public sealed class FileQueryInputDto : PagedRequestDto
{
	/// <summary>文件名模糊搜索（可选）</summary>
	public string? Keyword { get; set; }

	/// <summary>按访问级别筛选（可选）</summary>
	public int? AccessLevel { get; set; }

	/// <summary>按 MIME 类型前缀筛选（可选，如 "image/"）</summary>
	public string? ContentType { get; set; }
}
