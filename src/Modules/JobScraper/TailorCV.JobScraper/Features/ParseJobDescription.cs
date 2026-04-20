using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.JobScraper.Domain;
using TailorCV.JobScraper.Domain.Enums;
using TailorCV.JobScraper.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.JobScraper.Features;

public static class ParseJobDescription
{
    public record Request(string RawText);

    public record Response(Guid ParseId);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.RawText)
                .NotEmpty()
                .MinimumLength(50)
                .MaximumLength(10000);
        }
    }

    public class Handler(
        JobScraperDbContext dbContext,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            ParseJob parseJob = ParseJob.Create(
                currentUser.UserId,
                ParseJobType.ManualText,
                command.RawText,
                dateTimeProvider.UtcNow);

            dbContext.ParseJobs.Add(parseJob);
            await dbContext.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(parseJob.Id));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/jobs/parse", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/jobs/parse/{result.Value.ParseId}/status", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("JobScraper")
        .WithName("ParseJobDescription")
        .WithSummary("Parse a job description from raw text")
        .WithDescription("Triggers async AI parsing of a job description. Poll status endpoint for results.")
        .RequireAuthorization();
    }
}
