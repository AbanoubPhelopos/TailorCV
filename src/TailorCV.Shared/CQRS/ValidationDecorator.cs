using FluentValidation;
using FluentValidation.Results;
using TailorCV.Shared.Results;

namespace TailorCV.Shared.CQRS;

public class CommandValidationDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly IEnumerable<IValidator<TCommand>> _validators;

    public CommandValidationDecorator(
        ICommandHandler<TCommand, TResult> inner,
        IEnumerable<IValidator<TCommand>> validators)
    {
        _inner = inner;
        _validators = validators;
    }

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct)
    {
        List<ValidationFailure> failures = _validators
            .Select(v => v.Validate(command))
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            string errors = string.Join("; ", failures.Select(f => f.ErrorMessage));
            return Result<TResult>.Failure(Error.Validation(errors));
        }

        return await _inner.HandleAsync(command, ct);
    }
}

public class QueryValidationDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
{
    private readonly IQueryHandler<TQuery, TResult> _inner;
    private readonly IEnumerable<IValidator<TQuery>> _validators;

    public QueryValidationDecorator(
        IQueryHandler<TQuery, TResult> inner,
        IEnumerable<IValidator<TQuery>> validators)
    {
        _inner = inner;
        _validators = validators;
    }

    public async Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct)
    {
        List<ValidationFailure> failures = _validators
            .Select(v => v.Validate(query))
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            string errors = string.Join("; ", failures.Select(f => f.ErrorMessage));
            return Result<TResult>.Failure(Error.Validation(errors));
        }

        return await _inner.HandleAsync(query, ct);
    }
}
