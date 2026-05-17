#pragma warning disable CA1054
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

public static class GetGeneratedCV
{
    public record Request(Guid Id);

    public record Response(
        Guid Id,
        string GenerationType,
        string Status,
        string? ProfileSnapshot,
        string? JobSnapshot,
        Guid TemplateId,
        CVContent? Content,
        MatchScoreData? MatchScore,
        string? CoverLetter,
        string? TailoringPrompt,
        string PdfStatus,
        string? Error,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public class Handler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? cv = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == query.Id && c.UserId == userId, ct);

            if (cv is null)
            {
                return Result<Response>.Failure(CVErrors.CVNotFound);
            }

            return Result<Response>.Success(new Response(
                cv.Id,
                cv.GenerationType.ToString(),
                cv.Status.ToString(),
                cv.ProfileSnapshot,
                cv.JobSnapshot,
                cv.TemplateId,
                cv.Content is not null
                    ? JsonSerializer.Deserialize<CVContent>(cv.Content)
                    : null,
                cv.MatchScore is not null
                    ? JsonSerializer.Deserialize<MatchScoreData>(cv.MatchScore)
                    : null,
                cv.CoverLetter,
                cv.TailoringPrompt,
                cv.PdfStatus.ToString(),
                cv.Error,
                cv.CreatedAt,
                cv.UpdatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cv/{id:guid}", async (
            Guid id,
            IQueryHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(new Request(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("GetGeneratedCV")
        .WithSummary("Get generated CV details")
        .WithDescription("Returns full CV details including content, match score, cover letter, and PDF status. Use with template endpoint for client-side preview.")
        .Produces<Response>()
        .RequireAuthorization();
    }
}
