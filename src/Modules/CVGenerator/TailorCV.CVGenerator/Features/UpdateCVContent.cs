using FluentValidation;
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

public static class UpdateCVContent
{
    public record Request(
        Guid Id,
        string Summary,
        List<CVSection> Sections);

    public record Response(
        Guid Id,
        CVContent Content,
        MatchScoreData? MatchScore,
        string PdfStatus,
        DateTimeOffset UpdatedAt);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Summary).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Sections).NotEmpty();
        }
    }

    public class Handler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? cv = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == userId, ct);

            if (cv is null)
            {
                return Result<Response>.Failure(CVErrors.CVNotFound);
            }

            if (cv.Status != GenerationStatus.Done)
            {
                return Result<Response>.Failure(CVErrors.CVStillProcessing);
            }

            CVContent content = new(command.Summary, command.Sections);
            DateTimeOffset now = dateTimeProvider.UtcNow;
            string contentJson = JsonSerializer.Serialize(content);

            cv.UpdateContent(contentJson, now);
            await dbContext.SaveChangesAsync(ct);

            MatchScoreData? matchScore = cv.MatchScore is not null
                ? JsonSerializer.Deserialize<MatchScoreData>(cv.MatchScore)
                : null;

            return Result<Response>.Success(new Response(
                cv.Id,
                content,
                matchScore,
                cv.PdfStatus.ToString(),
                cv.UpdatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/cv/{id:guid}/content", async (
            Guid id,
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request with { Id = id }, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("UpdateCVContent")
        .WithSummary("Edit CV content")
        .WithDescription("Replaces the AI-generated CV content. Invalidates any cached PDF.")
        .Produces<Response>()
        .RequireAuthorization();
    }
}
