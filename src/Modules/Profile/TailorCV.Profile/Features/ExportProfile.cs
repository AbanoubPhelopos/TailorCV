#pragma warning disable CA1054, CA1305, CA1869

using System.Globalization;
using System.Text;
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

public static class ExportProfile
{
    private static readonly JsonSerializerOptions CachedJsonOptions = new() { WriteIndented = true };

    public record SectionExport(string SectionType, object Data);

    public record ExportDto(
        string Headline,
        string Summary,
        string Phone,
        string Location,
        string Website,
        string LinkedinUrl,
        string GithubUrl,
        List<SectionExport> Sections,
        DateTimeOffset ExportedAt);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<object, ExportDto>
    {
        public async Task<Result<ExportDto>> HandleAsync(object query, CancellationToken ct)
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
                return Result<ExportDto>.Failure(ProfileErrors.ProfileNotFound);
            }

            List<SectionExport> sections = [];

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
                    sections.Add(new SectionExport(order.SectionType.ToString(), data));
                }
            }

            return Result<ExportDto>.Success(new ExportDto(
                profile.Headline,
                profile.Summary,
                profile.Phone,
                profile.Location,
                profile.Website,
                profile.LinkedinUrl,
                profile.GithubUrl,
                sections,
                dateTimeProvider.UtcNow));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/profiles/me/export", async (
            IQueryHandler<object, ExportDto> handler,
            CancellationToken ct) =>
        {
            Result<ExportDto> result = await handler.HandleAsync(new object(), ct);
            if (result.IsFailure)
            {
                return result.ToProblemDetails();
            }

            string date = result.Value.ExportedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            return Results.Stream(
                new MemoryStream(Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(result.Value, CachedJsonOptions))),
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

#pragma warning restore CA1054, CA1305, CA1869
