using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Features.Shared;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Results;

#pragma warning disable CA1054, CA1308

namespace TailorCV.Profile.Features;

public static class GetSharedProfile
{
    public record Response(
        string FirstName,
        string LastName,
        string Headline,
        string Summary,
        string Location,
        string Website,
        string LinkedinUrl,
        string GithubUrl,
        List<SectionData> Sections);

    public record Request(string ShareId);

    public class Handler(
        ProfileDbContext dbContext) : IQueryHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request query, CancellationToken ct)
        {
            Domain.Profile? profile = await dbContext.Profiles
                .FirstOrDefaultAsync(p => p.ShareId == query.ShareId, ct);

            if (profile is null || !profile.IsShared)
            {
                return Result<Response>.Failure(ProfileErrors.ProfileNotFound);
            }

            ProfileUser? user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == profile.UserId, ct);

            return Result<Response>.Success(new Response(
                user?.FirstName ?? string.Empty,
                user?.LastName ?? string.Empty,
                profile.Headline, profile.Summary, profile.Location,
                profile.Website, profile.LinkedinUrl, profile.GithubUrl,
                profile.Sections.ToSectionDataList()));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/shared/{shareId}", async (
            string shareId,
            IQueryHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(new Request(shareId), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("Profile")
        .WithName("GetSharedProfile")
        .WithSummary("Get shared profile")
        .WithDescription("Public endpoint that returns a read-only visitor view of a shared profile. No authentication required.")
        .Produces<Response>();
    }
}
