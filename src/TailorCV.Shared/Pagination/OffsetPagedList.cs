using Microsoft.EntityFrameworkCore;

namespace TailorCV.Shared.Pagination;

public record PagingInfo(
    bool HasNext,
    bool HasPrevious,
    int Page,
    int PageSize,
    int Total);

public record PagingParams(int Page = 1, int PageSize = 10);

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

public static class OffsetPagedListExtensions
{
    public static async Task<OffsetPagedList<T>> ToOffsetPagedListAsync<T>(
        this IQueryable<T> query,
        PagingParams paging,
        CancellationToken ct = default)
    {
        int total = await query.CountAsync(ct);

        IReadOnlyList<T> items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(ct);

        return new OffsetPagedList<T>(items, paging.Page, paging.PageSize, total);
    }
}
