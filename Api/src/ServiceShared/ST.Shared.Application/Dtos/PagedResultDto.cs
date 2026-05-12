namespace ST.Shared.Application.Dtos;

public sealed class PagedResultDto<T>
{
	public required int PageIndex { get; init; }

	public required int PageSize { get; init; }

	public required long TotalCount { get; init; }

	public IReadOnlyList<T> Items { get; init; } = [];
}
