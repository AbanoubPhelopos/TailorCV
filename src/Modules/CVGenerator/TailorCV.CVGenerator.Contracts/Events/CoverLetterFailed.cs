namespace TailorCV.CVGenerator.Contracts.Events;

public record CoverLetterFailed(
    Guid GenerationId,
    string Error);
