namespace TailorCV.Profile.Contracts.Dto;

public record ParsedResumeData(
    string? Headline,
    string? Summary,
    string? Phone,
    string? Location,
    string? Website,
    string? Linkedin,
    string? Github,
    List<ParsedSection>? Sections);
