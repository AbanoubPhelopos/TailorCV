namespace TailorCV.Shared.Pagination;

public record PagingInfo(
    bool HasNext,
    bool HasPrevious,
    int Page,
    int PageSize,
    int Total);

public class OffsetPagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public PagingInfo PagingInfo { get; }

    public OffsetPagedList(IReadOnlyList<T> items, int page, int pageSize, int total)
    {
        Items = items;
        PagingInfo = new PagingInfo(
            HasNext: page * pageSize < total,
            HasPrevious: page > 1,
            Page: page,
            PageSize: pageSize,
            Total: total);
    }
}
