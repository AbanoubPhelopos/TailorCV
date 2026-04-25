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

public static class UpdateProfile
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public record SectionInput(string SectionType, JsonElement Data);

    public record Request(
        string? Headline,
        string? Summary,
        string? Phone,
        string? Location,
        string? Website,
        string? Linkedin,
        string? Github,
        List<SectionInput>? Sections);

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

            RuleForEach(x => x.Sections).ChildRules(section =>
            {
                section.RuleFor(s => s.SectionType)
                    .NotEmpty()
                    .Must(BeValidSectionType)
                    .WithMessage("Invalid section type");
            });
        }

        private static bool BeAValidUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static bool BeValidSectionType(string? sectionType)
        {
            string[] validTypes = ["Experience", "Project", "Skill", "Education", "Certification", "Language", "Custom"];
            return validTypes.Contains(sectionType);
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

            DateTimeOffset now = dateTimeProvider.UtcNow;

            profile.Update(
                command.Headline ?? string.Empty,
                command.Summary ?? string.Empty,
                command.Phone ?? string.Empty,
                command.Location ?? string.Empty,
                command.Website ?? string.Empty,
                command.Linkedin ?? string.Empty,
                command.Github ?? string.Empty,
                now);

            if (command.Sections is not null)
            {
                await ReplaceAllSections(dbContext, profile, command.Sections, ct);
            }

            await dbContext.SaveChangesAsync(ct);

            List<SectionOutput> sections = BuildSectionOutputs(profile);

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

        private static async Task ReplaceAllSections(
            ProfileDbContext dbContext,
            Domain.Profile profile,
            List<SectionInput> sections,
            CancellationToken ct)
        {
            dbContext.Experiences.RemoveRange(profile.Experiences);
            dbContext.Projects.RemoveRange(profile.Projects);
            dbContext.Skills.RemoveRange(profile.Skills);
            dbContext.Education.RemoveRange(profile.Education);
            dbContext.Certifications.RemoveRange(profile.Certifications);
            dbContext.Languages.RemoveRange(profile.Languages);
            dbContext.CustomSections.RemoveRange(profile.CustomSections);
            dbContext.SectionOrders.RemoveRange(profile.SectionOrders);

            await dbContext.SaveChangesAsync(ct);

            for (int i = 0; i < sections.Count; i++)
            {
                SectionInput section = sections[i];
                if (!Enum.TryParse<SectionType>(section.SectionType, out SectionType sectionType))
                {
                    continue;
                }

                Dictionary<string, object?> d = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                    section.Data.GetRawText())!;

                Guid sectionId = sectionType switch
                {
                    SectionType.Experience => CreateExperience(dbContext, profile.Id, d),
                    SectionType.Project => CreateProject(dbContext, profile.Id, d),
                    SectionType.Skill => CreateSkill(dbContext, profile.Id, d),
                    SectionType.Education => CreateEducation(dbContext, profile.Id, d),
                    SectionType.Certification => CreateCertification(dbContext, profile.Id, d),
                    SectionType.Language => CreateLanguage(dbContext, profile.Id, d),
                    SectionType.Custom => CreateCustomSection(dbContext, profile.Id, d),
                    _ => Guid.Empty,
                };

                if (sectionId != Guid.Empty)
                {
                    SectionOrder sectionOrder = SectionOrder.Create(profile.Id, sectionType, sectionId, i + 1);
                    dbContext.SectionOrders.Add(sectionOrder);
                }
            }
        }

        private static List<SectionOutput> BuildSectionOutputs(Domain.Profile profile)
        {
            List<SectionOutput> result = [];

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
                    result.Add(new SectionOutput(order.SectionType.ToString(), order.SectionId, order.Order, data));
                }
            }

            return result;
        }

        private static Guid CreateExperience(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
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
                return Guid.Empty;
            }

            dbContext.Experiences.Add(result.Value);
            return result.Value.Id;
        }

        private static Guid CreateProject(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string name = d.TryGetValue("name", out object? n) ? n?.ToString() ?? string.Empty : string.Empty;
            string description = d.TryGetValue("description", out object? desc) ? desc?.ToString() ?? string.Empty : string.Empty;
            string[] techStack = d.TryGetValue("techStack", out object? ts) && ts is not null
                ? JsonSerializer.Deserialize<string[]>(ts.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<string>()
                : Array.Empty<string>();
            string role = d.TryGetValue("role", out object? r) ? r?.ToString() ?? string.Empty : string.Empty;
            string projectLink = d.TryGetValue("url", out object? u) ? u?.ToString() ?? string.Empty : string.Empty;
            DateOnly? startDate = d.TryGetValue("startDate", out object? sd) && sd is not null && DateOnly.TryParse(sd.ToString(), CultureInfo.InvariantCulture, out DateOnly ps) ? ps : null;
            DateOnly? endDate = d.TryGetValue("endDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;

            Result<Project> result = Project.Create(profileId, name, description, techStack, role, projectLink, startDate, endDate);
            if (result.IsFailure)
            {
                return Guid.Empty;
            }

            dbContext.Projects.Add(result.Value);
            return result.Value.Id;
        }

        private static Guid CreateSkill(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string category = d.TryGetValue("category", out object? cat) ? cat?.ToString() ?? string.Empty : string.Empty;
            string[] items = d.TryGetValue("items", out object? it) && it is not null
                ? JsonSerializer.Deserialize<string[]>(it.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<string>()
                : Array.Empty<string>();

            Result<Skill> result = Skill.Create(profileId, category, items);
            if (result.IsFailure)
            {
                return Guid.Empty;
            }

            dbContext.Skills.Add(result.Value);
            return result.Value.Id;
        }

        private static Guid CreateEducation(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
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
                return Guid.Empty;
            }

            dbContext.Education.Add(result.Value);
            return result.Value.Id;
        }

        private static Guid CreateCertification(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string name = d.TryGetValue("name", out object? n) ? n?.ToString() ?? string.Empty : string.Empty;
            string issuer = d.TryGetValue("issuer", out object? iss) ? iss?.ToString() ?? string.Empty : string.Empty;
            DateOnly date = d.TryGetValue("date", out object? dt) && DateOnly.TryParse(dt?.ToString(), CultureInfo.InvariantCulture, out DateOnly pd) ? pd : default;
            DateOnly? expiryDate = d.TryGetValue("expiryDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;
            string credentialLink = d.TryGetValue("url", out object? u) ? u?.ToString() ?? string.Empty : string.Empty;

            Result<Certification> result = Certification.Create(profileId, name, issuer, date, expiryDate, credentialLink);
            if (result.IsFailure)
            {
                return Guid.Empty;
            }

            dbContext.Certifications.Add(result.Value);
            return result.Value.Id;
        }

        private static Guid CreateLanguage(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string languageName = d.TryGetValue("languageName", out object? ln) ? ln?.ToString() ?? string.Empty : string.Empty;
            LanguageProficiency proficiency = d.TryGetValue("proficiency", out object? prof) && Enum.TryParse(prof?.ToString(), out LanguageProficiency p) ? p : LanguageProficiency.Beginner;

            Result<Language> result = Language.Create(profileId, languageName, proficiency);
            if (result.IsFailure)
            {
                return Guid.Empty;
            }

            dbContext.Languages.Add(result.Value);
            return result.Value.Id;
        }

        private static Guid CreateCustomSection(ProfileDbContext dbContext, Guid profileId, Dictionary<string, object?> d)
        {
            string title = d.TryGetValue("title", out object? t) ? t?.ToString() ?? string.Empty : string.Empty;
            CustomSectionItem[] items = d.TryGetValue("items", out object? it) && it is not null
                ? JsonSerializer.Deserialize<CustomSectionItem[]>(it.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<CustomSectionItem>()
                : Array.Empty<CustomSectionItem>();

            Result<CustomSection> result = CustomSection.Create(profileId, title, items);
            if (result.IsFailure)
            {
                return Guid.Empty;
            }

            dbContext.CustomSections.Add(result.Value);
            return result.Value.Id;
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/profiles/me", async (
            Request request,
            ICommandHandler<Request, ProfileResponse> handler,
            CancellationToken ct) =>
        {
            Result<ProfileResponse> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("UpdateProfile")
        .WithSummary("Update user profile with sections")
        .WithDescription("Replaces the entire profile state including all sections. Frontend manages state and sends full snapshot.");
    }
}
