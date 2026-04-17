#pragma warning disable CA1873, S2139

using Microsoft.Extensions.Logging;
using TailorCV.Shared.Results;

namespace TailorCV.Shared.CQRS;

public class CommandLoggingDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult>
{
    private readonly ICommandHandler<TCommand, TResult> _inner;
    private readonly ILogger<CommandLoggingDecorator<TCommand, TResult>> _logger;

    public CommandLoggingDecorator(
        ICommandHandler<TCommand, TResult> inner,
        ILogger<CommandLoggingDecorator<TCommand, TResult>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct)
    {
        _logger.LogInformation("Handling {CommandType}", typeof(TCommand).Name);
        try
        {
            Result<TResult> result = await _inner.HandleAsync(command, ct);
            _logger.LogInformation("Handled {CommandType}: {Status}", typeof(TCommand).Name,
                result.IsSuccess ? "Success" : "Failure");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {CommandType}", typeof(TCommand).Name);
            throw;
        }
    }
}

public class QueryLoggingDecorator<TQuery, TResult> : IQueryHandler<TQuery, TResult>
{
    private readonly IQueryHandler<TQuery, TResult> _inner;
    private readonly ILogger<QueryLoggingDecorator<TQuery, TResult>> _logger;

    public QueryLoggingDecorator(
        IQueryHandler<TQuery, TResult> inner,
        ILogger<QueryLoggingDecorator<TQuery, TResult>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<Result<TResult>> HandleAsync(TQuery query, CancellationToken ct)
    {
        _logger.LogInformation("Handling {QueryType}", typeof(TQuery).Name);
        try
        {
            Result<TResult> result = await _inner.HandleAsync(query, ct);
            _logger.LogInformation("Handled {QueryType}: {Status}", typeof(TQuery).Name,
                result.IsSuccess ? "Success" : "Failure");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {QueryType}", typeof(TQuery).Name);
            throw;
        }
    }
}

#pragma warning restore CA1873, S2139
