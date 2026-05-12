namespace ST.Shared.Application.Dtos;

public class PagedRequestDto
{
	private const int DefaultPageIndex = 1;
	private const int DefaultPageSize = 20;
	private const int MaxPageSize = 100;

	public int PageIndex { get; set; } = DefaultPageIndex;

	public int PageSize { get; set; } = DefaultPageSize;

	public (int PageIndex, int PageSize, int Skip) Normalize()
	{
		var pageIndex = PageIndex <= 0 ? DefaultPageIndex : PageIndex;
		var pageSize = PageSize <= 0 ? DefaultPageSize : Math.Min(PageSize, MaxPageSize);
		var skip = (pageIndex - 1) * pageSize;
		return (pageIndex, pageSize, skip);
	}
}
