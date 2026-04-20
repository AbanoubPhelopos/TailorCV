#pragma warning disable CA1054, S6580, S1172

using System.Globalization;
using System.Text.Json;
using FluentValidation;
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

public static class ImportResumeConfirm
{
    public record SectionImport(string SectionType, object Data);

    public record Request(
        string? Headline,
        string? Summary,
        string? Phone,
        string? Location,
        string? Website,
        string? LinkedinUrl,
        string? GithubUrl,
        List<SectionImport> Sections);

    public record Response(Guid ProfileId, int SectionsImported, int Completeness);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;
            DateTimeOffset now = dateTimeProvider.UtcNow;

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
                Result<Domain.Profile> createResult = Domain.Profile.Create(
                    userId,
                    command.Headline ?? string.Empty,
                    command.Summary ?? string.Empty,
                    command.Phone ?? string.Empty,
                    command.Location ?? string.Empty,
                    command.Website ?? string.Empty,
                    command.LinkedinUrl ?? string.Empty,
                    command.GithubUrl ?? string.Empty,
                    now);

                if (createResult.IsFailure)
                {
                    return Result<Response>.Failure(createResult.Error);
                }

                profile = createResult.Value;
                dbContext.Profiles.Add(profile);
            }
            else
            {
                profile.Update(
                    !string.IsNullOrWhiteSpace(command.Headline) ? command.Headline : profile.Headline,
                    !string.IsNullOrWhiteSpace(command.Summary) ? command.Summary : profile.Summary,
                    !string.IsNullOrWhiteSpace(command.Phone) ? command.Phone : profile.Phone,
                    !string.IsNullOrWhiteSpace(command.Location) ? command.Location : profile.Location,
                    !string.IsNullOrWhiteSpace(command.Website) ? command.Website : profile.Website,
                    !string.IsNullOrWhiteSpace(command.LinkedinUrl) ? command.LinkedinUrl : profile.LinkedinUrl,
                    !string.IsNullOrWhiteSpace(command.GithubUrl) ? command.GithubUrl : profile.GithubUrl,
                    now);
            }

            int maxOrder = profile.SectionOrders.Count != 0
                ? profile.SectionOrders.Max(s => s.Order)
                : 0;

            int imported = 0;

            if (command.Sections is not null)
            {
                foreach (SectionImport section in command.Sections)
                {
                    if (!Enum.TryParse<SectionType>(section.SectionType, out SectionType sectionType))
                    {
                        continue;
                    }

                    maxOrder++;
                    Dictionary<string, object?> d = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        JsonSerializer.Serialize(section.Data))!;

                    Result<(Guid SectionId, Error? Error)> sectionResult = sectionType switch
                    {
                        SectionType.Experience => CreateExperience(dbContext, profile.Id, d),
                        SectionType.Project => CreateProject(dbContext, profile.Id, d),
                        SectionType.Skill => CreateSkill(dbContext, profile.Id, d),
                        SectionType.Education => CreateEducation(dbContext, profile.Id, d),
                        SectionType.Certification => CreateCertification(dbContext, profile.Id, d),
                        SectionType.Language => CreateLanguage(dbContext, profile.Id, d),
                        SectionType.Custom => CreateCustomSection(dbContext, profile.Id, d),
                        _ => Result<(Guid, Error?)>.Failure(Error.Validation("Invalid section type")),
                    };

                    if (sectionResult.IsSuccess && sectionResult.Value.Error is null)
                    {
                        SectionOrder sectionOrder = SectionOrder.Create(profile.Id, sectionType, sectionResult.Value.SectionId, maxOrder);
                        dbContext.SectionOrders.Add(sectionOrder);
                        imported++;
                    }
                }
            }

            await dbContext.SaveChangesAsync(ct);

            int completeness = profile.CalculateCompleteness();

            return Result<Response>.Success(new Response(profile.Id, imported, completeness));
        }

        private static Result<(Guid, Error?)> CreateExperience(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string company = d.TryGetValue("company", out object? c) ? c?.ToString() ?? string.Empty : string.Empty;
            string role = d.TryGetValue("role", out object? r) ? r?.ToString() ?? string.Empty : string.Empty;
            DateOnly startDate = d.TryGetValue("startDate", out object? sd) && DateOnly.TryParse(sd?.ToString(), CultureInfo.InvariantCulture, out DateOnly ps) ? ps : default;
            DateOnly? endDate = d.TryGetValue("endDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;
            string description = d.TryGetValue("description", out object? desc) ? desc?.ToString() ?? string.Empty : string.Empty;
            bool isCurrent = d.TryGetValue("isCurrent", out object? ic) && ic is not null && bool.TryParse(ic.ToString(), out bool parsed) && parsed;

            Result<Experience> result = Experience.Create(profileId, company, role, startDate, endDate, description, isCurrent);
            if (result.IsFailure)
            {
                return Result<(Guid, Error?)>.Failure(result.Error);
            }

            dbContext.Experiences.Add(result.Value);
            return Result<(Guid, Error?)>.Success((result.Value.Id, null));
        }

        private static Result<(Guid, Error?)> CreateProject(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string name = d.TryGetValue("name", out object? n) ? n?.ToString() ?? string.Empty : string.Empty;
            string description = d.TryGetValue("description", out object? desc) ? desc?.ToString() ?? string.Empty : string.Empty;
            string[] techStack = d.TryGetValue("techStack", out object? ts) && ts is not null
                ? JsonSerializer.Deserialize<string[]>(ts.ToString()!) ?? Array.Empty<string>()
                : Array.Empty<string>();
            string role = d.TryGetValue("role", out object? r) ? r?.ToString() ?? string.Empty : string.Empty;
            string url = d.TryGetValue("url", out object? u) ? u?.ToString() ?? string.Empty : string.Empty;
            DateOnly? startDate = d.TryGetValue("startDate", out object? sd) && sd is not null && DateOnly.TryParse(sd.ToString(), CultureInfo.InvariantCulture, out DateOnly ps) ? ps : null;
            DateOnly? endDate = d.TryGetValue("endDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;

            Result<Project> result = Project.Create(profileId, name, description, techStack, role, url, startDate, endDate);
            if (result.IsFailure)
            {
                return Result<(Guid, Error?)>.Failure(result.Error);
            }

            dbContext.Projects.Add(result.Value);
            return Result<(Guid, Error?)>.Success((result.Value.Id, null));
        }

        private static Result<(Guid, Error?)> CreateSkill(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string category = d.TryGetValue("category", out object? cat) ? cat?.ToString() ?? string.Empty : string.Empty;
            string[] items = d.TryGetValue("items", out object? it) && it is not null
                ? JsonSerializer.Deserialize<string[]>(it.ToString()!) ?? Array.Empty<string>()
                : Array.Empty<string>();

            Result<Skill> result = Skill.Create(profileId, category, items);
            if (result.IsFailure)
            {
                return Result<(Guid, Error?)>.Failure(result.Error);
            }

            dbContext.Skills.Add(result.Value);
            return Result<(Guid, Error?)>.Success((result.Value.Id, null));
        }

        private static Result<(Guid, Error?)> CreateEducation(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string institution = d.TryGetValue("institution", out object? i) ? i?.ToString() ?? string.Empty : string.Empty;
            string degree = d.TryGetValue("degree", out object? deg) ? deg?.ToString() ?? string.Empty : string.Empty;
            string field = d.TryGetValue("field", out object? f) ? f?.ToString() ?? string.Empty : string.Empty;
            DateOnly startDate = d.TryGetValue("startDate", out object? sd) && DateOnly.TryParse(sd?.ToString(), CultureInfo.InvariantCulture, out DateOnly ps) ? ps : default;
            DateOnly? endDate = d.TryGetValue("endDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;
            string gpa = d.TryGetValue("gpa", out object? g) ? g?.ToString() ?? string.Empty : string.Empty;

            Result<Education> result = Education.Create(profileId, institution, degree, field, startDate, endDate, gpa);
            if (result.IsFailure)
            {
                return Result<(Guid, Error?)>.Failure(result.Error);
            }

            dbContext.Education.Add(result.Value);
            return Result<(Guid, Error?)>.Success((result.Value.Id, null));
        }

        private static Result<(Guid, Error?)> CreateCertification(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string name = d.TryGetValue("name", out object? n) ? n?.ToString() ?? string.Empty : string.Empty;
            string issuer = d.TryGetValue("issuer", out object? iss) ? iss?.ToString() ?? string.Empty : string.Empty;
            DateOnly date = d.TryGetValue("date", out object? dt) && DateOnly.TryParse(dt?.ToString(), CultureInfo.InvariantCulture, out DateOnly pd) ? pd : default;
            DateOnly? expiryDate = d.TryGetValue("expiryDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;
            string url = d.TryGetValue("url", out object? u) ? u?.ToString() ?? string.Empty : string.Empty;

            Result<Certification> result = Certification.Create(profileId, name, issuer, date, expiryDate, url);
            if (result.IsFailure)
            {
                return Result<(Guid, Error?)>.Failure(result.Error);
            }

            dbContext.Certifications.Add(result.Value);
            return Result<(Guid, Error?)>.Success((result.Value.Id, null));
        }

        private static Result<(Guid, Error?)> CreateLanguage(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string languageName = d.TryGetValue("languageName", out object? ln) ? ln?.ToString() ?? string.Empty : string.Empty;
            LanguageProficiency proficiency = d.TryGetValue("proficiency", out object? prof) && Enum.TryParse(prof?.ToString(), out LanguageProficiency p) ? p : LanguageProficiency.Beginner;

            Result<Language> result = Language.Create(profileId, languageName, proficiency);
            if (result.IsFailure)
            {
                return Result<(Guid, Error?)>.Failure(result.Error);
            }

            dbContext.Languages.Add(result.Value);
            return Result<(Guid, Error?)>.Success((result.Value.Id, null));
        }

        private static Result<(Guid, Error?)> CreateCustomSection(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string title = d.TryGetValue("title", out object? t) ? t?.ToString() ?? string.Empty : string.Empty;
            CustomSectionItem[] items = d.TryGetValue("items", out object? it) && it is not null
                ? JsonSerializer.Deserialize<CustomSectionItem[]>(it.ToString()!) ?? Array.Empty<CustomSectionItem>()
                : Array.Empty<CustomSectionItem>();

            Result<CustomSection> result = CustomSection.Create(profileId, title, items);
            if (result.IsFailure)
            {
                return Result<(Guid, Error?)>.Failure(result.Error);
            }

            dbContext.CustomSections.Add(result.Value);
            return Result<(Guid, Error?)>.Success((result.Value.Id, null));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profiles/me/import/confirm", async (
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
        .WithName("ImportResumeConfirm")
        .WithSummary("Confirm resume import")
        .WithDescription("Confirms the parsed resume data and saves it to the profile. Creates profile if it doesn't exist.");
    }
}

#pragma warning restore CA1054, S6580, S1172
