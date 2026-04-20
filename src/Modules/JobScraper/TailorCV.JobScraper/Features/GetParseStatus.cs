using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.JobScraper.Domain;
using TailorCV.JobScraper.Domain.Enums;
using TailorCV.JobScraper.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Results;

namespace TailorCV.JobScraper.Features;

public static class GetParseStatus
{
    public record Request(Guid ParseId);

    public record Response(
        string Status,
        ParsedJobData? ParsedJob = null,
        string? Error = null);

    public class Handler(JobScraperDbContext dbContext) : IQueryHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request query, CancellationToken ct)
        {
            ParseJob? parseJob = await dbContext.ParseJobs
                .FirstOrDefaultAsync(p => p.Id == query.ParseId, ct);

            if (parseJob is null)
            {
                return Result<Response>.Failure(JobScraperErrors.ParseJobNotFound);
            }

            return parseJob.Status switch
            {
                ParseJobStatus.Done => Result<Response>.Success(
                    new Response("DONE", parseJob.ParsedData)),
                ParseJobStatus.Failed => Result<Response>.Success(
                    new Response("FAILED", Error: parseJob.Error)),
                _ => Result<Response>.Success(
                    new Response(parseJob.Status.ToString().ToUpperInvariant()))
            };
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/jobs/parse/{parseId}/status", async (
            Guid parseId,
            IQueryHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(new Request(parseId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("JobScraper")
        .WithName("GetParseStatus")
        .WithSummary("Poll parse job status")
        .WithDescription("Returns PROCESSING, DONE with parsed data, or FAILED with error message.")
        .RequireAuthorization();
    }
}
