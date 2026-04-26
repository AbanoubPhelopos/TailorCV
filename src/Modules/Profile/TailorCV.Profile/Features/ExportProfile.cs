using System.Globalization;
using System.Text;
using System.Text.Json;
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

public static class ExportProfile
{
    public record Response(
        string Headline,
        string Summary,
        string Phone,
        string Location,
        string Website,
        string LinkedinUrl,
        string GithubUrl,
        List<SectionData> Sections,
        DateTimeOffset ExportedAt);

    public record Request;

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Request, Response>
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
                profile.Headline, profile.Summary, profile.Phone, profile.Location,
                profile.Website, profile.LinkedinUrl, profile.GithubUrl,
                profile.Sections.ToSectionDataList(),
                dateTimeProvider.UtcNow));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/me/export", async (
                IQueryHandler<Request, Response> handler,
                CancellationToken ct) =>
            {
                Result<Response> result = await handler.HandleAsync(new Request(), ct);
                if (result.IsFailure)
                {
                    return result.ToProblemDetails();
                }

                string date = result.Value.ExportedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result.Value));
                return Results.Stream(
                    new MemoryStream(bytes),
                    "application/json",
                    $"profile_export_{date}.json");
            })
            .RequireAuthorization()
            .WithTags("Profile")
            .WithName("ExportProfile")
            .WithSummary("Export profile as JSON")
            .WithDescription("Exports the user's full profile data as a downloadable JSON file.");
    }
}
