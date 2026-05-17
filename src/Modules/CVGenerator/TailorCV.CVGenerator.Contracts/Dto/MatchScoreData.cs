namespace TailorCV.CVGenerator.Contracts.Dto;

public record MatchScoreData(
    int Percentage,
    List<string> MatchingSkills,
    List<string> MissingSkills);
