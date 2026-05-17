#pragma warning disable CA1308
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Dto;
using TailorCV.CVGenerator.Domain;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.CVGenerator.Features;

public static class GetGenerationStatus
{
    public record Request(Guid GenerationId);

    public record GenerationStatusResponse(
        GenerationStatus Status,
        GeneratedCVSummary? GeneratedCv,
        string? Error);

    public record GeneratedCVSummary(
        Guid Id,
        MatchScoreData? MatchScore,
        string? CoverLetter,
        DateTimeOffset CreatedAt);

    public class Handler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<Request, GenerationStatusResponse>
    {
        public async Task<Result<GenerationStatusResponse>> HandleAsync(Request query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? cv = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == query.GenerationId && c.UserId == userId, ct);

            if (cv is null)
            {
                return Result<GenerationStatusResponse>.Failure(CVErrors.CVNotFound);
            }

            if (cv.Status == GenerationStatus.Done)
            {
                MatchScoreData? matchScore = cv.MatchScore is not null
                    ? JsonSerializer.Deserialize<MatchScoreData>(cv.MatchScore)
                    : null;

                return Result<GenerationStatusResponse>.Success(new GenerationStatusResponse(
                    cv.Status,
                    new GeneratedCVSummary(cv.Id, matchScore, cv.CoverLetter, cv.CreatedAt),
                    null));
            }

            if (cv.Status == GenerationStatus.Failed)
            {
                return Result<GenerationStatusResponse>.Success(new GenerationStatusResponse(
                    cv.Status,
                    null,
                    cv.Error ?? "Unknown error"));
            }

            return Result<GenerationStatusResponse>.Success(new GenerationStatusResponse(
                cv.Status,
                null,
                null));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cv/generate/{generationId:guid}/status", async (
            Guid generationId,
            IQueryHandler<Request, GenerationStatusResponse> handler,
            CancellationToken ct) =>
        {
            Result<GenerationStatusResponse> result = await handler.HandleAsync(new Request(generationId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("GetGenerationStatus")
        .WithSummary("Poll CV generation status")
        .WithDescription("Returns the current status of a CV generation job. Poll until DONE or FAILED.")
        .Produces<GenerationStatusResponse>()
        .RequireAuthorization();
    }
}
