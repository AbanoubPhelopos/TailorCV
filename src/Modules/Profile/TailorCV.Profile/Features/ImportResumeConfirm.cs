using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Contracts.Events;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Features.Shared;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using Wolverine;

namespace TailorCV.Profile.Features;

public static class ImportResumeConfirm
{
    public record Request(
        string? Headline,
        string? Summary,
        string? Phone,
        string? Location,
        string? Website,
        string? Linkedin,
        string? Github,
        List<SectionData>? Sections = null);

    public record Response(Guid ProfileId, int SectionsImported, int Completeness);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMessageBus bus) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;
            DateTimeOffset now = dateTimeProvider.UtcNow;

            Domain.Profile? profile = await dbContext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                Result<Domain.Profile> createResult = Domain.Profile.Create(
                    userId,
                    command.Headline ?? string.Empty,
                    command.Summary ?? string.Empty,
                    command.Phone ?? string.Empty,
                    command.Location ?? string.Empty,
                    command.Website ?? string.Empty,
                    command.Linkedin ?? string.Empty,
                    command.Github ?? string.Empty,
                    now);

                if (createResult.IsFailure)
                {
                    return Result<Response>.Failure(createResult.Error);
                }

                profile = createResult.Value;
                dbContext.Profiles.Add(profile);
            }
            else
            {
                profile.Update(
                    !string.IsNullOrWhiteSpace(command.Headline) ? command.Headline : profile.Headline,
                    !string.IsNullOrWhiteSpace(command.Summary) ? command.Summary : profile.Summary,
                    !string.IsNullOrWhiteSpace(command.Phone) ? command.Phone : profile.Phone,
                    !string.IsNullOrWhiteSpace(command.Location) ? command.Location : profile.Location,
                    !string.IsNullOrWhiteSpace(command.Website) ? command.Website : profile.Website,
                    !string.IsNullOrWhiteSpace(command.Linkedin) ? command.Linkedin : profile.LinkedinUrl,
                    !string.IsNullOrWhiteSpace(command.Github) ? command.Github : profile.GithubUrl,
                    now);
            }

            int imported = 0;

            if (command.Sections is not null)
            {
                int maxOrder = profile.Sections.Count != 0 ? profile.Sections.Max(s => s.Order) : 0;

                foreach (SectionData data in command.Sections)
                {
                    maxOrder++;
                    ProfileSection section = data.ToProfileSection();
                    section.Order = maxOrder;
                    profile.Sections.Add(section);
                    imported += section.Items.Count;
                }
            }

            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new ProfileUpdated(userId, profile.Id, profile.UpdatedAt));

            return Result<Response>.Success(new Response(profile.Id, imported, profile.Completeness));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profiles/me/import/confirm", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("ImportResumeConfirm")
        .WithSummary("Confirm resume import")
        .WithDescription("Confirms the parsed resume data and saves it to the profile. Creates profile if it doesn't exist.")
        .Produces<Response>();
    }
}
