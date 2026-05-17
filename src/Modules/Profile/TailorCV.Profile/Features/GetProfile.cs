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

#pragma warning disable CA1054, CA1308

namespace TailorCV.Profile.Features;

public static class GetProfile
{
    public record Request;

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

            return Result<Response>.Success(new Response(
                profile.Id,
                profile.Headline,
                profile.Summary,
                profile.Phone,
                profile.Location,
                profile.Website,
                profile.LinkedinUrl,
                profile.GithubUrl,
                profile.Completeness,
                profile.Sections.ToSectionDataList(),
                profile.CreatedAt,
                profile.UpdatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/me", async (
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
        .WithName("GetProfile")
        .WithSummary("Get current user profile")
        .WithDescription("Returns the authenticated user's full profile with all sections.")
        .Produces<Response>();
    }
}
