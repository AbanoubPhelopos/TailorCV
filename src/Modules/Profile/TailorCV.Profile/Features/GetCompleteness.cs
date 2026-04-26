using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Features.Shared;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class GetCompleteness
{
    public record Check(string Field, bool Passed, string? Suggestion, int? Count = null);

    public record Response(int Percentage, bool HasProfile, List<Check> Checks);

    public record Request;

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            Domain.Profile? profile = await dbContext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                return Result<Response>.Failure(ProfileErrors.ProfileNotFound);
            }

            List<Check> checks =
            [
                new("headline", !string.IsNullOrWhiteSpace(profile.Headline),
                    !string.IsNullOrWhiteSpace(profile.Headline) ? null : "Add a professional headline"),

                new("summary", !string.IsNullOrWhiteSpace(profile.Summary) && profile.Summary.Length >= 50,
                    !string.IsNullOrWhiteSpace(profile.Summary) && profile.Summary.Length >= 50 ? null : "Write a summary about yourself"),

                new("phone", !string.IsNullOrWhiteSpace(profile.Phone),
                    !string.IsNullOrWhiteSpace(profile.Phone) ? null : "Add a phone number"),

                new("location", !string.IsNullOrWhiteSpace(profile.Location),
                    !string.IsNullOrWhiteSpace(profile.Location) ? null : "Add your location"),

                new("experience", profile.Sections.Any(s => s.Type == "experience" && s.Items.Count != 0),
                    profile.Sections.Any(s => s.Type == "experience") ? null : "Add your work experience",
                    profile.Sections.Where(s => s.Type == "experience").Sum(s => s.Items.Count)),

                new("projects", profile.Sections.Any(s => s.Type == "project" && s.Items.Count != 0),
                    profile.Sections.Any(s => s.Type == "project") ? null : "Showcase your projects",
                    profile.Sections.Where(s => s.Type == "project").Sum(s => s.Items.Count)),

                new("skills", profile.Sections.Any(s => s.Type == "skill" && s.Items.Count != 0),
                    profile.Sections.Any(s => s.Type == "skill") ? null : "Add your skills",
                    profile.Sections.Where(s => s.Type == "skill").Sum(s => s.Items.Count)),

                new("education", profile.Sections.Any(s => s.Type == "education" && s.Items.Count != 0),
                    profile.Sections.Any(s => s.Type == "education") ? null : "Add your education",
                    profile.Sections.Where(s => s.Type == "education").Sum(s => s.Items.Count)),

                new("certifications", profile.Sections.Any(s => s.Type == "certification" && s.Items.Count != 0),
                    profile.Sections.Any(s => s.Type == "certification") ? null : "Consider adding certifications",
                    profile.Sections.Where(s => s.Type == "certification").Sum(s => s.Items.Count)),

                new("languages", profile.Sections.Any(s => s.Type == "language" && s.Items.Count != 0),
                    profile.Sections.Any(s => s.Type == "language") ? null : "Add languages you speak",
                    profile.Sections.Where(s => s.Type == "language").Sum(s => s.Items.Count)),
            ];

            return Result<Response>.Success(new Response(profile.Completeness, true, checks));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/me/completeness", async (
            IQueryHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(new Request(), ct);
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
