namespace TailorCV.JobDescriptions.Contracts.Dto;

public record ParsedJobDataDto(
    string Title,
    string? Company,
    string? Location,
    List<string> RequiredSkills,
    List<string> Responsibilities,
    List<string> Qualifications,
    string SeniorityLevel);