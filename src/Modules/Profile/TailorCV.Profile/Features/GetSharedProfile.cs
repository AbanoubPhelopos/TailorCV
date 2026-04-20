#pragma warning disable CA1054

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Domain.Enums;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class GetSharedProfile
{
    public record VisitorSectionResponse(string SectionType, object Data);

    public record VisitorResponse(
        string FirstName,
        string LastName,
        string Headline,
        string Summary,
        string Location,
        string Website,
        string LinkedinUrl,
        string GithubUrl,
        List<VisitorSectionResponse> Sections);

    public class Handler(
        ProfileDbContext dbContext) : IQueryHandler<string, VisitorResponse>
    {
        public async Task<Result<VisitorResponse>> HandleAsync(string shareId, CancellationToken ct)
        {
            Domain.Profile? profile = await dbContext.Profiles
                .Include(p => p.Experiences)
                .Include(p => p.Projects)
                .Include(p => p.Skills)
                .Include(p => p.Education)
                .Include(p => p.Certifications)
                .Include(p => p.Languages)
                .Include(p => p.CustomSections)
                .Include(p => p.SectionOrders)
                .FirstOrDefaultAsync(p => p.ShareId == shareId, ct);

            if (profile is null || !profile.IsShared)
            {
                return Result<VisitorResponse>.Failure(ProfileErrors.ProfileNotFound);
            }

            List<VisitorSectionResponse> sections = [];

            foreach (SectionOrder order in profile.SectionOrders.OrderBy(s => s.Order))
            {
                object? data = order.SectionType switch
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

                if (data is not null)
                {
                    sections.Add(new VisitorSectionResponse(order.SectionType.ToString(), data));
                }
            }

            return Result<VisitorResponse>.Success(new VisitorResponse(
                string.Empty,
                string.Empty,
                profile.Headline,
                profile.Summary,
                profile.Location,
                profile.Website,
                profile.LinkedinUrl,
                profile.GithubUrl,
                sections));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/shared/{shareId}", async (
            string shareId,
            IQueryHandler<string, VisitorResponse> handler,
            CancellationToken ct) =>
        {
            Result<VisitorResponse> result = await handler.HandleAsync(shareId, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("Profile")
        .WithName("GetSharedProfile")
        .WithSummary("Get shared profile")
        .WithDescription("Public endpoint that returns a read-only visitor view of a shared profile. No authentication required.");
    }
}

#pragma warning restore CA1054
