#pragma warning disable CA1054, S6580, S1172

using System.Globalization;
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

public static class UpdateSection
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public record SectionRequest(string SectionType, object Data);

    public record Request(Guid SectionId, string SectionType, object Data);

    public record Response(Guid SectionId, string SectionType);

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

            SectionOrder? sectionOrder = await dbContext.SectionOrders
                .FirstOrDefaultAsync(so => so.SectionId == command.SectionId, ct);

            if (sectionOrder is null)
            {
                return Result<Response>.Failure(ProfileErrors.SectionNotFound);
            }

            if (sectionOrder.ProfileId != profile.Id)
            {
                return Result<Response>.Failure(ProfileErrors.SectionNotOwned);
            }

            SectionType requestedType = Enum.Parse<SectionType>(command.SectionType);
            if (sectionOrder.SectionType != requestedType)
            {
                return Result<Response>.Failure(ProfileErrors.SectionTypeMismatch);
            }

            Dictionary<string, object?> d = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                JsonSerializer.Serialize(command.Data))!;

            Result updateResult = await (sectionOrder.SectionType switch
            {
                SectionType.Experience => UpdateExperience(dbContext, sectionOrder.SectionId, d, ct),
                SectionType.Project => UpdateProject(dbContext, sectionOrder.SectionId, d, ct),
                SectionType.Skill => UpdateSkill(dbContext, sectionOrder.SectionId, d, ct),
                SectionType.Education => UpdateEducation(dbContext, sectionOrder.SectionId, d, ct),
                SectionType.Certification => UpdateCertification(dbContext, sectionOrder.SectionId, d, ct),
                SectionType.Language => UpdateLanguage(dbContext, sectionOrder.SectionId, d, ct),
                SectionType.Custom => UpdateCustomSection(dbContext, sectionOrder.SectionId, d, ct),
                _ => Task.FromResult(Result.Failure(Error.Validation("Invalid section type"))),
            });

            if (updateResult.IsFailure)
            {
                return Result<Response>.Failure(updateResult.Error);
            }

            await dbContext.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                command.SectionId,
                command.SectionType));
        }

        private static async Task<Result> UpdateExperience(
            ProfileDbContext dbContext, Guid sectionId, Dictionary<string, object?> d, CancellationToken ct)
        {
            Experience? entity = await dbContext.Experiences.FindAsync([sectionId], ct);
            if (entity is null)
            {
                return Result.Failure(ProfileErrors.SectionNotFound);
            }

            string company = d.TryGetValue("company", out object? c) ? c?.ToString() ?? string.Empty : string.Empty;
            string role = d.TryGetValue("role", out object? r) ? r?.ToString() ?? string.Empty : string.Empty;
            DateOnly startDate = d.TryGetValue("startDate", out object? sd) && DateOnly.TryParse(sd?.ToString(), CultureInfo.InvariantCulture, out DateOnly ps) ? ps : entity.StartDate;
            DateOnly? endDate = d.TryGetValue("endDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;
            string description = d.TryGetValue("description", out object? desc) ? desc?.ToString() ?? string.Empty : string.Empty;
            bool isCurrent = d.TryGetValue("isCurrent", out object? ic) && ic is not null && bool.TryParse(ic.ToString(), out bool parsed) && parsed;

            entity.Update(company, role, startDate, endDate, description, isCurrent);
            return Result.Success();
        }

        private static async Task<Result> UpdateProject(
            ProfileDbContext dbContext, Guid sectionId, Dictionary<string, object?> d, CancellationToken ct)
        {
            Project? entity = await dbContext.Projects.FindAsync([sectionId], ct);
            if (entity is null)
            {
                return Result.Failure(ProfileErrors.SectionNotFound);
            }

            string name = d.TryGetValue("name", out object? n) ? n?.ToString() ?? string.Empty : string.Empty;
            string description = d.TryGetValue("description", out object? desc) ? desc?.ToString() ?? string.Empty : string.Empty;
            string[] techStack = d.TryGetValue("techStack", out object? ts) && ts is not null
                ? JsonSerializer.Deserialize<string[]>(ts.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<string>()
                : Array.Empty<string>();
            string role = d.TryGetValue("role", out object? r) ? r?.ToString() ?? string.Empty : string.Empty;
            string url = d.TryGetValue("url", out object? u) ? u?.ToString() ?? string.Empty : string.Empty;
            DateOnly? startDate = d.TryGetValue("startDate", out object? sd) && sd is not null && DateOnly.TryParse(sd.ToString(), CultureInfo.InvariantCulture, out DateOnly ps) ? ps : null;
            DateOnly? endDate = d.TryGetValue("endDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;

            entity.Update(name, description, techStack, role, url, startDate, endDate);
            return Result.Success();
        }

        private static async Task<Result> UpdateSkill(
            ProfileDbContext dbContext, Guid sectionId, Dictionary<string, object?> d, CancellationToken ct)
        {
            Skill? entity = await dbContext.Skills.FindAsync([sectionId], ct);
            if (entity is null)
            {
                return Result.Failure(ProfileErrors.SectionNotFound);
            }

            string category = d.TryGetValue("category", out object? cat) ? cat?.ToString() ?? string.Empty : string.Empty;
            string[] items = d.TryGetValue("items", out object? it) && it is not null
                ? JsonSerializer.Deserialize<string[]>(it.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<string>()
                : Array.Empty<string>();

            entity.Update(category, items);
            return Result.Success();
        }

        private static async Task<Result> UpdateEducation(
            ProfileDbContext dbContext, Guid sectionId, Dictionary<string, object?> d, CancellationToken ct)
        {
            Education? entity = await dbContext.Education.FindAsync([sectionId], ct);
            if (entity is null)
            {
                return Result.Failure(ProfileErrors.SectionNotFound);
            }

            string institution = d.TryGetValue("institution", out object? i) ? i?.ToString() ?? string.Empty : string.Empty;
            string degree = d.TryGetValue("degree", out object? deg) ? deg?.ToString() ?? string.Empty : string.Empty;
            string field = d.TryGetValue("field", out object? f) ? f?.ToString() ?? string.Empty : string.Empty;
            DateOnly startDate = d.TryGetValue("startDate", out object? sd) && DateOnly.TryParse(sd?.ToString(), CultureInfo.InvariantCulture, out DateOnly ps) ? ps : entity.StartDate;
            DateOnly? endDate = d.TryGetValue("endDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;
            string gpa = d.TryGetValue("gpa", out object? g) ? g?.ToString() ?? string.Empty : string.Empty;

            entity.Update(institution, degree, field, startDate, endDate, gpa);
            return Result.Success();
        }

        private static async Task<Result> UpdateCertification(
            ProfileDbContext dbContext, Guid sectionId, Dictionary<string, object?> d, CancellationToken ct)
        {
            Certification? entity = await dbContext.Certifications.FindAsync([sectionId], ct);
            if (entity is null)
            {
                return Result.Failure(ProfileErrors.SectionNotFound);
            }

            string name = d.TryGetValue("name", out object? n) ? n?.ToString() ?? string.Empty : string.Empty;
            string issuer = d.TryGetValue("issuer", out object? iss) ? iss?.ToString() ?? string.Empty : string.Empty;
            DateOnly date = d.TryGetValue("date", out object? dt) && DateOnly.TryParse(dt?.ToString(), CultureInfo.InvariantCulture, out DateOnly pd) ? pd : entity.Date;
            DateOnly? expiryDate = d.TryGetValue("expiryDate", out object? ed) && ed is not null && DateOnly.TryParse(ed.ToString(), CultureInfo.InvariantCulture, out DateOnly pe) ? pe : null;
            string url = d.TryGetValue("url", out object? u) ? u?.ToString() ?? string.Empty : string.Empty;

            entity.Update(name, issuer, date, expiryDate, url);
            return Result.Success();
        }

        private static async Task<Result> UpdateLanguage(
            ProfileDbContext dbContext, Guid sectionId, Dictionary<string, object?> d, CancellationToken ct)
        {
            Language? entity = await dbContext.Languages.FindAsync([sectionId], ct);
            if (entity is null)
            {
                return Result.Failure(ProfileErrors.SectionNotFound);
            }

            string languageName = d.TryGetValue("languageName", out object? ln) ? ln?.ToString() ?? string.Empty : string.Empty;
            LanguageProficiency proficiency = d.TryGetValue("proficiency", out object? prof) && Enum.TryParse(prof?.ToString(), out LanguageProficiency p) ? p : entity.Proficiency;

            entity.Update(languageName, proficiency);
            return Result.Success();
        }

        private static async Task<Result> UpdateCustomSection(
            ProfileDbContext dbContext, Guid sectionId, Dictionary<string, object?> d, CancellationToken ct)
        {
            CustomSection? entity = await dbContext.CustomSections.FindAsync([sectionId], ct);
            if (entity is null)
            {
                return Result.Failure(ProfileErrors.SectionNotFound);
            }

            string title = d.TryGetValue("title", out object? t) ? t?.ToString() ?? string.Empty : string.Empty;
            CustomSectionItem[] items = d.TryGetValue("items", out object? it) && it is not null
                ? JsonSerializer.Deserialize<CustomSectionItem[]>(it.ToString()!, CaseInsensitiveOptions) ?? Array.Empty<CustomSectionItem>()
                : Array.Empty<CustomSectionItem>();

            entity.Update(title, items);
            return Result.Success();
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/profiles/me/sections/{sectionId:guid}", async (
            Guid sectionId,
            SectionRequest sectionRequest,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Request request = new(sectionId, sectionRequest.SectionType, sectionRequest.Data);
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("UpdateSection")
        .WithSummary("Update a profile section")
        .WithDescription("Updates an existing section. Section type cannot be changed.");
    }
}

#pragma warning restore CA1054, S6580, S1172
