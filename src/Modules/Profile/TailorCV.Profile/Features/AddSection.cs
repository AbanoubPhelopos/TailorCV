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

public static class AddSection
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public record Request(string SectionType, object Data);

    public record Response(Guid SectionId, string SectionType, int Order);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SectionType)
                .NotEmpty()
                .Must(BeValidSectionType)
                .WithMessage("Invalid section type. Valid types: Experience, Project, Skill, Education, Certification, Language, Custom");

            RuleFor(x => x.Data)
                .NotNull();
        }

        private static bool BeValidSectionType(string? sectionType)
        {
            string[] validTypes = ["Experience", "Project", "Skill", "Education", "Certification", "Language", "Custom"];
            return validTypes.Contains(sectionType);
        }
    }

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            Domain.Profile? profile = await dbContext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                return Result<Response>.Failure(ProfileErrors.ProfileNotFound);
            }

            SectionType sectionType = Enum.Parse<SectionType>(command.SectionType);

            int maxOrder = await dbContext.SectionOrders
                .Where(s => s.ProfileId == profile.Id)
                .Select(s => (int?)s.Order)
                .MaxAsync(ct) ?? 0;

            int nextOrder = maxOrder + 1;

            Result<(Guid SectionId, Error? Error)> sectionResult = sectionType switch
            {
                SectionType.Experience => CreateExperience(dbContext, profile.Id, command.Data),
                SectionType.Project => CreateProject(dbContext, profile.Id, command.Data),
                SectionType.Skill => CreateSkill(dbContext, profile.Id, command.Data),
                SectionType.Education => CreateEducation(dbContext, profile.Id, command.Data),
                SectionType.Certification => CreateCertification(dbContext, profile.Id, command.Data),
                SectionType.Language => CreateLanguage(dbContext, profile.Id, command.Data),
                SectionType.Custom => CreateCustomSection(dbContext, profile.Id, command.Data),
                _ => Result<(Guid, Error?)>.Failure(Error.Validation("Invalid section type")),
            };

            if (sectionResult.IsFailure || sectionResult.Value.Error is not null)
            {
                Error error = sectionResult.IsFailure ? sectionResult.Error : sectionResult.Value.Error!;
                return Result<Response>.Failure(error);
            }

            Guid sectionId = sectionResult.Value.SectionId;

            SectionOrder sectionOrder = SectionOrder.Create(profile.Id, sectionType, sectionId, nextOrder);
            dbContext.SectionOrders.Add(sectionOrder);

            await dbContext.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(sectionId, command.SectionType, nextOrder));
        }

        private static Dictionary<string, object?> ToDict(object data)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(data))!;
        }

        private static Result<(Guid, Error?)> CreateExperience(ProfileDbContext dbContext, Guid profileId, object data)
        {
            Dictionary<string, object?> d = ToDict(data);

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

        private static Result<(Guid, Error?)> CreateProject(ProfileDbContext dbContext, Guid profileId, object data)
        {
            Dictionary<string, object?> d = ToDict(data);

            string name = d.TryGetValue("name", out object? n) ? n?.ToString() ?? string.Empty : string.Empty;
            string description = d.TryGetValue("description", out object? desc) ? desc?.ToString() ?? string.Empty : string.Empty;
            string[] techStack = d.TryGetValue("techStack", out object? ts) && ts is not null
                ? JsonSerializer.Deserialize<string[]>(ts.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<string>()
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

        private static Result<(Guid, Error?)> CreateSkill(ProfileDbContext dbContext, Guid profileId, object data)
        {
            Dictionary<string, object?> d = ToDict(data);

            string category = d.TryGetValue("category", out object? cat) ? cat?.ToString() ?? string.Empty : string.Empty;
            string[] items = d.TryGetValue("items", out object? it) && it is not null
                ? JsonSerializer.Deserialize<string[]>(it.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<string>()
                : Array.Empty<string>();

            Result<Skill> result = Skill.Create(profileId, category, items);
            if (result.IsFailure)
            {
                return Result<(Guid, Error?)>.Failure(result.Error);
            }

            dbContext.Skills.Add(result.Value);
            return Result<(Guid, Error?)>.Success((result.Value.Id, null));
        }

        private static Result<(Guid, Error?)> CreateEducation(ProfileDbContext dbContext, Guid profileId, object data)
        {
            Dictionary<string, object?> d = ToDict(data);

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

        private static Result<(Guid, Error?)> CreateCertification(ProfileDbContext dbContext, Guid profileId, object data)
        {
            Dictionary<string, object?> d = ToDict(data);

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

        private static Result<(Guid, Error?)> CreateLanguage(ProfileDbContext dbContext, Guid profileId, object data)
        {
            Dictionary<string, object?> d = ToDict(data);

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

        private static Result<(Guid, Error?)> CreateCustomSection(ProfileDbContext dbContext, Guid profileId, object data)
        {
            Dictionary<string, object?> d = ToDict(data);

            string title = d.TryGetValue("title", out object? t) ? t?.ToString() ?? string.Empty : string.Empty;
            CustomSectionItem[] items = d.TryGetValue("items", out object? it) && it is not null
                ? JsonSerializer.Deserialize<CustomSectionItem[]>(it.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<CustomSectionItem>()
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
        app.MapPost("/api/profiles/me/sections", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/profiles/me/sections/{result.Value.SectionId}", result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("AddSection")
        .WithSummary("Add a section to profile")
        .WithDescription("Adds a new section (Experience, Project, Skill, Education, Certification, Language, Custom) to the user's profile.");
    }
}

#pragma warning restore CA1054, S6580, S1172
