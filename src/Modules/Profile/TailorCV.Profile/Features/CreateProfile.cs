using System.Text.Json;
using FluentValidation;
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

public static class CreateProfile
{
    public record Request(
        string? Headline,
        string? Summary,
        string? Phone,
        string? Location,
        string? Website,
        string? Linkedin,
        string? Github);

    public record ProfileResponse(
        Guid Id,
        string Headline,
        string Summary,
        string Phone,
        string Location,
        string Website,
        string Linkedin,
        string Github,
        int Completeness,
        List<SectionOutput> Sections,
        DateTimeOffset CreatedAt);

    public record SectionOutput(string SectionType, Guid SectionId, int Order, JsonElement Data);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Headline)
                .MaximumLength(200)
                .When(x => x.Headline is not null);

            RuleFor(x => x.Summary)
                .MaximumLength(2000)
                .When(x => x.Summary is not null);

            RuleFor(x => x.Phone)
                .MaximumLength(50)
                .When(x => x.Phone is not null);

            RuleFor(x => x.Location)
                .MaximumLength(200)
                .When(x => x.Location is not null);

            RuleFor(x => x.Website)
                .Must(BeAValidUrl)
                .WithMessage("Website must be a valid URL")
                .When(x => !string.IsNullOrWhiteSpace(x.Website));

            RuleFor(x => x.Linkedin)
                .Must(BeAValidUrl)
                .WithMessage("LinkedIn URL must be a valid URL")
                .When(x => !string.IsNullOrWhiteSpace(x.Linkedin));

            RuleFor(x => x.Github)
                .Must(BeAValidUrl)
                .WithMessage("GitHub URL must be a valid URL")
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
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, ProfileResponse>
    {
        public async Task<Result<ProfileResponse>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            bool exists = await dbContext.Profiles
                .AnyAsync(p => p.UserId == userId, ct);

            if (exists)
            {
                return Result<ProfileResponse>.Failure(ProfileErrors.ProfileAlreadyExists);
            }

            DateTimeOffset now = dateTimeProvider.UtcNow;

            Result<Domain.Profile> profileResult = Domain.Profile.Create(
                userId,
                command.Headline ?? string.Empty,
                command.Summary ?? string.Empty,
                command.Phone ?? string.Empty,
                command.Location ?? string.Empty,
                command.Website ?? string.Empty,
                command.Linkedin ?? string.Empty,
                command.Github ?? string.Empty,
                now);

            if (profileResult.IsFailure)
            {
                return Result<ProfileResponse>.Failure(profileResult.Error);
            }

            Domain.Profile profile = profileResult.Value;

            dbContext.Profiles.Add(profile);
            await dbContext.SaveChangesAsync(ct);

            return Result<ProfileResponse>.Success(new ProfileResponse(
                profile.Id,
                profile.Headline,
                profile.Summary,
                profile.Phone,
                profile.Location,
                profile.Website,
                profile.LinkedinUrl,
                profile.GithubUrl,
                profile.Completeness,
                [],
                profile.CreatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profiles", async (
            Request request,
            ICommandHandler<Request, ProfileResponse> handler,
            CancellationToken ct) =>
        {
            Result<ProfileResponse> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/profiles/me", result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("CreateProfile")
        .WithSummary("Create user profile")
        .WithDescription("Creates a new professional profile for the authenticated user. One profile per user.");
    }
}
