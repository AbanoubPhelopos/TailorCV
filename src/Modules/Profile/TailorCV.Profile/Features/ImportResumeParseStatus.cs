using System.Text.Json;
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
    public record ParseStatusResponse(string Status, JsonElement? ParsedData = null, string? Error = null);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<Guid, ParseStatusResponse>
    {
        public async Task<Result<ParseStatusResponse>> HandleAsync(Guid parseId, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            ParseJob? parseJob = await dbContext.ParseJobs
                .FirstOrDefaultAsync(p => p.Id == parseId && p.UserId == userId, ct);

            if (parseJob is null)
            {
                return Result<ParseStatusResponse>.Failure(ProfileErrors.ParseJobNotFound);
            }

            return parseJob.Status switch
            {
                Domain.Enums.ParseJobStatus.Queued => Result<ParseStatusResponse>.Success(new ParseStatusResponse("QUEUED")),
                Domain.Enums.ParseJobStatus.Processing => Result<ParseStatusResponse>.Success(new ParseStatusResponse("PROCESSING")),
                Domain.Enums.ParseJobStatus.Done => Result<ParseStatusResponse>.Success(new ParseStatusResponse("DONE", parseJob.ParsedData?.RootElement)),
                Domain.Enums.ParseJobStatus.Failed => Result<ParseStatusResponse>.Success(new ParseStatusResponse("FAILED", Error: parseJob.Error)),
                _ => Result<ParseStatusResponse>.Failure(Error.Validation("Unknown parse job status")),
            };
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/me/import/parse/{parseId:guid}/status", async (
            Guid parseId,
            IQueryHandler<Guid, ParseStatusResponse> handler,
            CancellationToken ct) =>
        {
            Result<ParseStatusResponse> result = await handler.HandleAsync(parseId, ct);
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
