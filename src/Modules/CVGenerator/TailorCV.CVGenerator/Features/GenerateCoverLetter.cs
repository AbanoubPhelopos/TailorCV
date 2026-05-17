using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Commands;
using TailorCV.CVGenerator.Contracts.Dto;
using TailorCV.CVGenerator.Domain;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using Wolverine;

namespace TailorCV.CVGenerator.Features;

public static class GenerateCoverLetter
{
    public record Request(Guid Id);

    public record TriggerResponse(Guid GenerationId);

    public record CoverLetterStatusResponse(string Status, string? CoverLetter = null, string? Error = null);

    public class TriggerHandler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService,
        IMessageBus bus) : ICommandHandler<Request, TriggerResponse>
    {
        public async Task<Result<TriggerResponse>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? cv = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == userId, ct);

            if (cv is null)
            {
                return Result<TriggerResponse>.Failure(CVErrors.CVNotFound);
            }

            if (cv.Status != GenerationStatus.Done)
            {
                return Result<TriggerResponse>.Failure(CVErrors.CVStillProcessing);
            }

            await bus.PublishAsync(new TailorCoverLetter(
                cv.Id,
                cv.ProfileSnapshot,
                cv.JobSnapshot,
                cv.TailoringPrompt));

            return Result<TriggerResponse>.Success(new TriggerResponse(cv.Id));
        }
    }

    public class StatusHandler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<Request, CoverLetterStatusResponse>
    {
        public async Task<Result<CoverLetterStatusResponse>> HandleAsync(Request query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? cv = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == query.Id && c.UserId == userId, ct);

            if (cv is null)
            {
                return Result<CoverLetterStatusResponse>.Failure(CVErrors.CVNotFound);
            }

            if (cv.Status == GenerationStatus.Queued || cv.Status == GenerationStatus.Processing)
            {
                return Result<CoverLetterStatusResponse>.Success(new CoverLetterStatusResponse("PROCESSING"));
            }

            if (cv.Status == GenerationStatus.Failed)
            {
                return Result<CoverLetterStatusResponse>.Success(
                    new CoverLetterStatusResponse("FAILED", Error: cv.Error));
            }

            if (cv.CoverLetter is not null)
            {
                return Result<CoverLetterStatusResponse>.Success(
                    new CoverLetterStatusResponse("DONE", cv.CoverLetter));
            }

            return Result<CoverLetterStatusResponse>.Success(new CoverLetterStatusResponse("PROCESSING"));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cv/{id:guid}/cover-letter", async (
            Guid id,
            ICommandHandler<Request, TriggerResponse> handler,
            CancellationToken ct) =>
        {
            Result<TriggerResponse> result = await handler.HandleAsync(new Request(id), ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/cv/{id}/cover-letter/status", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("GenerateCoverLetter")
        .WithSummary("Generate cover letter")
        .WithDescription("Generates a cover letter for an existing generated CV using stored snapshots.")
        .Produces<TriggerResponse>(202)
        .RequireAuthorization();

        app.MapGet("/api/cv/{id:guid}/cover-letter/status", async (
            Guid id,
            IQueryHandler<Request, CoverLetterStatusResponse> handler,
            CancellationToken ct) =>
        {
            Result<CoverLetterStatusResponse> result = await handler.HandleAsync(new Request(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("GetCoverLetterStatus")
        .WithSummary("Poll cover letter generation status")
        .WithDescription("Returns the current status of cover letter generation.")
        .Produces<CoverLetterStatusResponse>()
        .RequireAuthorization();
    }
}
