using TailorCV.Shared.Results;

namespace TailorCV.Shared.CQRS;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct);
}
