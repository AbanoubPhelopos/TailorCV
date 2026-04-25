using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Domain.Enums;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class GetProfile
{
    public record SectionOutput(string SectionType, Guid SectionId, int Order, JsonElement Data);

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
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<object, ProfileResponse>
    {
        public async Task<Result<ProfileResponse>> HandleAsync(object query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            Domain.Profile? profile = await dbContext.Profiles
                .Include(p => p.Experiences)
                .Include(p => p.Projects)
                .Include(p => p.Skills)
                .Include(p => p.Education)
                .Include(p => p.Certifications)
                .Include(p => p.Languages)
                .Include(p => p.CustomSections)
                .Include(p => p.SectionOrders)
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                return Result<ProfileResponse>.Failure(ProfileErrors.ProfileNotFound);
            }

            List<SectionOutput> sections = [];

            foreach (SectionOrder order in profile.SectionOrders.OrderBy(s => s.Order))
            {
                object? entity = order.SectionType switch
                {
                    SectionType.Experience => (object?)profile.Experiences.FirstOrDefault(e => e.Id == order.SectionId),
                    SectionType.Project => profile.Projects.FirstOrDefault(p => p.Id == order.SectionId),
                    SectionType.Skill => profile.Skills.FirstOrDefault(s => s.Id == order.SectionId),
                    SectionType.Education => profile.Education.FirstOrDefault(e => e.Id == order.SectionId),
                    SectionType.Certification => profile.Certifications.FirstOrDefault(c => c.Id == order.SectionId),
                    SectionType.Language => profile.Languages.FirstOrDefault(l => l.Id == order.SectionId),
                    SectionType.Custom => profile.CustomSections.FirstOrDefault(c => c.Id == order.SectionId),
                    _ => null,
                };

                if (entity is not null)
                {
                    JsonElement data = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(entity));
                    sections.Add(new SectionOutput(
                        order.SectionType.ToString(),
                        order.SectionId,
                        order.Order,
                        data));
                }
            }

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
                sections,
                profile.CreatedAt,
                profile.UpdatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/me", async (
            IQueryHandler<object, ProfileResponse> handler,
            CancellationToken ct) =>
        {
            Result<ProfileResponse> result = await handler.HandleAsync(new object(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("GetProfile")
        .WithSummary("Get current user profile")
        .WithDescription("Returns the authenticated user's full profile with all sections ordered by SectionOrder.");
    }
}
