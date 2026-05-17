using System.Text.Json.Serialization;

#pragma warning disable CA1054

namespace TailorCV.Profile.Features.Shared;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ExperienceSectionData), "experience")]
[JsonDerivedType(typeof(ProjectSectionData), "project")]
[JsonDerivedType(typeof(SkillSectionData), "skill")]
[JsonDerivedType(typeof(EducationSectionData), "education")]
[JsonDerivedType(typeof(CertificationSectionData), "certification")]
[JsonDerivedType(typeof(LanguageSectionData), "language")]
[JsonDerivedType(typeof(CustomSectionData), "custom")]
public abstract record SectionData(
    Guid Id,
    int Order);

public record ExperienceSectionData(
    Guid Id,
    int Order,
    List<ExperienceItem> Items) : SectionData(Id, Order);

public record ExperienceItem(
    Guid Id,
    int Order,
    string Company,
    string Role,
    DateOnly StartDate,
    DateOnly? EndDate = null,
    string? Description = null,
    bool IsCurrent = false);

public record ProjectSectionData(
    Guid Id,
    int Order,
    List<ProjectItem> Items) : SectionData(Id, Order);

public record ProjectItem(
    Guid Id,
    int Order,
    string Name,
    string? Description = null,
    List<string>? TechStack = null,
    string? Role = null,
    string? Url = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null);

public record SkillSectionData(
    Guid Id,
    int Order,
    List<SkillItem> Items) : SectionData(Id, Order);

public record SkillItem(
    Guid Id,
    int Order,
    string Name);

public record EducationSectionData(
    Guid Id,
    int Order,
    List<EducationItem> Items) : SectionData(Id, Order);

public record EducationItem(
    Guid Id,
    int Order,
    string Institution,
    string Degree,
    string Field,
    DateOnly StartDate,
    DateOnly? EndDate = null,
    string? Gpa = null);

public record CertificationSectionData(
    Guid Id,
    int Order,
    List<CertificationItem> Items) : SectionData(Id, Order);

public record CertificationItem(
    Guid Id,
    int Order,
    string Name,
    string Issuer,
    DateOnly Date,
    DateOnly? ExpiryDate = null,
    string? Url = null);

public record LanguageSectionData(
    Guid Id,
    int Order,
    List<LanguageItem> Items) : SectionData(Id, Order);

public record LanguageItem(
    Guid Id,
    int Order,
    string LanguageName,
    string Proficiency);

public record CustomSectionData(
    Guid Id,
    int Order,
    string Title,
    List<CustomItem> Items) : SectionData(Id, Order);

public record CustomItem(
    Guid Id,
    int Order,
    string Title,
    string? Subtitle = null,
    List<string>? Description = null,
    string? Url = null);
