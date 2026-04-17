using TailorCV.Shared.Results;

namespace TailorCV.Shared.CQRS;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct);
}
