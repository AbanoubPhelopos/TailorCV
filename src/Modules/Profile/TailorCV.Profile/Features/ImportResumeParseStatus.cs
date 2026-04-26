using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class ImportResumeParseStatus
{
    public record Response(string Status, ParsedResumeData? ParsedData = null, string? Error = null);

    public record Request(Guid ParseId);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            ParseJob? parseJob = await dbContext.ParseJobs
                .FirstOrDefaultAsync(p => p.Id == query.ParseId && p.UserId == userId, ct);

            if (parseJob is null)
            {
                return Result<Response>.Failure(ProfileErrors.ParseJobNotFound);
            }

            return parseJob.Status switch
            {
                Domain.Enums.ParseJobStatus.Queued => Result<Response>.Success(new Response("QUEUED")),
                Domain.Enums.ParseJobStatus.Processing => Result<Response>.Success(new Response("PROCESSING")),
                Domain.Enums.ParseJobStatus.Done => Result<Response>.Success(new Response("DONE", parseJob.ParsedData)),
                Domain.Enums.ParseJobStatus.Failed => Result<Response>.Success(new Response("FAILED", Error: parseJob.Error)),
                _ => Result<Response>.Failure(Error.Validation("Unknown parse job status")),
            };
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/me/import/parse/{parseId:guid}/status", async (
            Guid parseId,
            IQueryHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(new Request(parseId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("ImportResumeParseStatus")
        .WithSummary("Poll parse status")
        .WithDescription("Returns the status of a resume parsing job. Poll until DONE or FAILED.");
    }
}
