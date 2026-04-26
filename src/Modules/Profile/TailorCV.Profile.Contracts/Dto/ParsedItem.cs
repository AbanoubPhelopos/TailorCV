#pragma warning disable CA1054

namespace TailorCV.Profile.Contracts.Dto;

public record ParsedItem(
    int Order,
    string? Company,
    string? Role,
    string? Description,
    bool? IsCurrent,
    string? StartDate,
    string? EndDate,
    string? Name,
    List<string>? TechStack,
    string? Url,
    string? Category,
    List<string>? SkillItems,
    string? Institution,
    string? Degree,
    string? Field,
    string? Gpa,
    string? Issuer,
    string? Date,
    string? ExpiryDate,
    string? LanguageName,
    string? Proficiency,
    string? Title,
    string? Subtitle);
