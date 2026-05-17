using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.CVGenerator.Contracts.Commands;
using TailorCV.CVGenerator.Domain;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using Wolverine;

namespace TailorCV.CVGenerator.Features;

public static class RegenerateCV
{
    public record Request(Guid Id, Guid TemplateId, string? TailoringPrompt);

    public record Response(Guid GenerationId);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.TemplateId).NotEmpty();
            RuleFor(x => x.TailoringPrompt)
                .MaximumLength(2000)
                .When(x => x.TailoringPrompt is not null);
        }
    }

    public class Handler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMessageBus bus) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            GeneratedCV? original = await dbContext.GeneratedCVs
                .FirstOrDefaultAsync(c => c.Id == command.Id && c.UserId == userId, ct);

            if (original is null)
            {
                return Result<Response>.Failure(CVErrors.CVNotFound);
            }

            if (original.Status != GenerationStatus.Done)
            {
                return Result<Response>.Failure(CVErrors.CVStillProcessing);
            }

            DateTimeOffset now = dateTimeProvider.UtcNow;

            GeneratedCV regenerated = GeneratedCV.Create(
                userId,
                original.ProfileSnapshot,
                original.JobSnapshot,
                command.TemplateId,
                GenerationType.FullCV,
                command.TailoringPrompt ?? original.TailoringPrompt,
                now);

            dbContext.GeneratedCVs.Add(regenerated);
            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new Contracts.Commands.TailorCV(
                regenerated.Id,
                userId,
                regenerated.ProfileSnapshot,
                regenerated.JobSnapshot,
                command.TemplateId,
                false,
                regenerated.TailoringPrompt));

            return Result<Response>.Success(new Response(regenerated.Id));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/cv/{id:guid}/regenerate", async (
            Guid id,
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request with { Id = id }, ct);
            return result.IsSuccess
                ? Results.Accepted($"/api/cv/generate/{result.Value.GenerationId}/status", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("RegenerateCV")
        .WithSummary("Regenerate CV with new template")
        .WithDescription("Creates a new CV using the original profile/JD snapshots with a different template or prompt. Original CV is preserved.")
        .Produces<Response>(202)
        .RequireAuthorization();
    }
}
