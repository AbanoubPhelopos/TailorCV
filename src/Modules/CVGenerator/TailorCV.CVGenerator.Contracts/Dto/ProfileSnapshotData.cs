namespace TailorCV.CVGenerator.Contracts.Dto;

public record ProfileSnapshotData(
    string Headline,
    string Summary,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Location,
    List<ProfileSectionSnapshot> Sections);

public record ProfileSectionSnapshot(
    string Type,
    string? Title,
    List<SectionItemSnapshot> Items);

public record SectionItemSnapshot(
    string? Company,
    string? Role,
    string? Description,
    bool IsCurrent,
    string? StartDate,
    string? EndDate,
    string? Name,
    List<string>? TechStack,
    string? Category,
    List<string>? SkillItems,
    string? Institution,
    string? Degree,
    string? Field,
    string? Issuer,
    string? LanguageName,
    string? Proficiency);
