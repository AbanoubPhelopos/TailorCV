namespace TailorCV.CVGenerator.Contracts.Dto;

public record CVContent(
    string Summary,
    List<CVSection> Sections);

public record CVSection(
    string Type,
    string Title,
    List<string> Items);
