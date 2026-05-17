namespace TailorCV.CVGenerator.Contracts.Dto;

public record JobSnapshotData(
    string Title,
    string Company,
    string? Location,
    List<string> RequiredSkills,
    List<string> Responsibilities,
    List<string> Qualifications,
    string? SeniorityLevel);
