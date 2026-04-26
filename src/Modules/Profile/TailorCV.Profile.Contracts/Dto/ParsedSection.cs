namespace TailorCV.Profile.Contracts.Dto;

public record ParsedSection(
    string Type,
    int Order,
    List<ParsedItem> Items);
