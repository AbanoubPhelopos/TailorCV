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

#pragma warning disable CA1054, CA1308

namespace TailorCV.Profile.Features;

public static class UpdateProfile
{
    public record Request(
        string? Headline,
        string? Summary,
        string? Phone,
        string? Location,
        string? Website,
        string? Linkedin,
        string? Github);

    public record Response(
        Guid Id,
        string Headline,
        string Summary,
        string Phone,
        string Location,
        string Website,
        string LinkedinUrl,
        string GithubUrl,
        int Completeness,
        List<SectionData> Sections,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Headline).MaximumLength(200).When(x => x.Headline is not null);
            RuleFor(x => x.Summary).MaximumLength(2000).When(x => x.Summary is not null);
            RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone is not null);
            RuleFor(x => x.Location).MaximumLength(200).When(x => x.Location is not null);

            RuleFor(x => x.Website).Must(BeAValidUrl).WithMessage("Website must be a valid URL")
                .When(x => !string.IsNullOrWhiteSpace(x.Website));
            RuleFor(x => x.Linkedin).Must(BeAValidUrl).WithMessage("LinkedIn URL must be a valid URL")
                .When(x => !string.IsNullOrWhiteSpace(x.Linkedin));
            RuleFor(x => x.Github).Must(BeAValidUrl).WithMessage("GitHub URL must be a valid URL")
                .When(x => !string.IsNullOrWhiteSpace(x.Github));
        }

        private static bool BeAValidUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IMessageBus bus) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            Domain.Profile? profile = await dbContext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                return Result<Response>.Failure(ProfileErrors.ProfileNotFound);
            }

            profile.Update(
                command.Headline ?? string.Empty,
                command.Summary ?? string.Empty,
                command.Phone ?? string.Empty,
                command.Location ?? string.Empty,
                command.Website ?? string.Empty,
                command.Linkedin ?? string.Empty,
                command.Github ?? string.Empty,
                dateTimeProvider.UtcNow);

            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new ProfileUpdated(userId, profile.Id, profile.UpdatedAt));

            return Result<Response>.Success(new Response(
                profile.Id, profile.Headline, profile.Summary, profile.Phone,
                profile.Location, profile.Website, profile.LinkedinUrl, profile.GithubUrl,
                profile.Completeness, profile.Sections.ToSectionDataList(),
                profile.CreatedAt, profile.UpdatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/profiles/me", async (
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
        .WithName("UpdateProfile")
        .WithSummary("Update profile base fields")
        .WithDescription("Updates the profile's base fields (headline, summary, contact info, URLs). Sections are managed via PUT /api/profiles/me/sections.");
    }
}
