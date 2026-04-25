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

public static class GetCompleteness
{
    public record CompletenessCheck(string Field, bool Passed, string? Suggestion, int? Count = null);

    public record CompletenessResponse(int Percentage, bool HasProfile, List<CompletenessCheck> Checks);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<object, CompletenessResponse>
    {
        public async Task<Result<CompletenessResponse>> HandleAsync(object query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            Domain.Profile? profile = await dbContext.Profiles
                .Include(p => p.Experiences)
                .Include(p => p.Projects)
                .Include(p => p.Skills)
                .Include(p => p.Education)
                .Include(p => p.Certifications)
                .Include(p => p.Languages)
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                return Result<CompletenessResponse>.Failure(ProfileErrors.ProfileNotFound);
            }

            List<CompletenessCheck> checks =
            [
                new("headline", !string.IsNullOrWhiteSpace(profile.Headline),
                    !string.IsNullOrWhiteSpace(profile.Headline) ? null : "Add a professional headline"),

                new("summary", !string.IsNullOrWhiteSpace(profile.Summary) && profile.Summary.Length >= 50,
                    !string.IsNullOrWhiteSpace(profile.Summary) && profile.Summary.Length >= 50 ? null : "Write a summary about yourself"),

                new("phone", !string.IsNullOrWhiteSpace(profile.Phone),
                    !string.IsNullOrWhiteSpace(profile.Phone) ? null : "Add a phone number"),

                new("location", !string.IsNullOrWhiteSpace(profile.Location),
                    !string.IsNullOrWhiteSpace(profile.Location) ? null : "Add your location"),

                new("experience", profile.Experiences.Count != 0,
                    profile.Experiences.Count != 0 ? null : "Add your work experience", profile.Experiences.Count),

                new("projects", profile.Projects.Count != 0,
                    profile.Projects.Count != 0 ? null : "Showcase your projects", profile.Projects.Count),

                new("skills", profile.Skills.Count != 0,
                    profile.Skills.Count != 0 ? null : "Add your skills", profile.Skills.Count),

                new("education", profile.Education.Count != 0,
                    profile.Education.Count != 0 ? null : "Add your education", profile.Education.Count),

                new("certifications", profile.Certifications.Count != 0,
                    profile.Certifications.Count != 0 ? null : "Consider adding certifications", profile.Certifications.Count),

                new("languages", profile.Languages.Count != 0,
                    profile.Languages.Count != 0 ? null : "Add languages you speak", profile.Languages.Count),
            ];

            return Result<CompletenessResponse>.Success(new CompletenessResponse(profile.Completeness, true, checks));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/me/completeness", async (
            IQueryHandler<object, CompletenessResponse> handler,
            CancellationToken ct) =>
        {
            Result<CompletenessResponse> result = await handler.HandleAsync(new object(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("GetCompleteness")
        .WithSummary("Get profile completeness")
        .WithDescription("Returns profile completeness percentage and a list of checks with suggestions.");
    }
}
