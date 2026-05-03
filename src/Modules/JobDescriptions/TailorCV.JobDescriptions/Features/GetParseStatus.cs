using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.JobDescriptions.Domain;
using TailorCV.JobDescriptions.Domain.Enums;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.JobDescriptions.Features;

public static class GetParseStatus
{
    public record Request(Guid ParseId);

    public record Response(
        ParseJobStatus Status,
        ParsedJobData? ParsedJob = null,
        string? Error = null,
        string? RawText = null,
        Uri? SourceUrl = null);

    public class Handler(JobDescriptionsDbContext dbContext, ICurrentUserService currentUser) : IQueryHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request query, CancellationToken ct)
        {
            ParseJob? parseJob = await dbContext.ParseJobs
                .FirstOrDefaultAsync(p => p.Id == query.ParseId && p.UserId == currentUser.UserId, ct);

            if (parseJob is null)
            {
                return Result<Response>.Failure(JobDescriptionErrors.ParseJobNotFound);
            }

            return parseJob.Status switch
            {
                ParseJobStatus.Done => Result<Response>.Success(
                    new Response(ParseJobStatus.Done, parseJob.ParsedData, RawText: parseJob.RawText, SourceUrl: parseJob.SourceUrl)),
                ParseJobStatus.Failed => Result<Response>.Success(
                    new Response(ParseJobStatus.Failed, Error: parseJob.Error)),
                _ => Result<Response>.Success(
                    new Response(parseJob.Status, RawText: parseJob.RawText, SourceUrl: parseJob.SourceUrl))
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
        .WithTags("JobDescription")
        .WithName("GetParseStatus")
        .WithSummary("Poll parse job status")
        .WithDescription("Returns PROCESSING, DONE with parsed data, or FAILED with error message.")
        .RequireAuthorization();
    }
}
