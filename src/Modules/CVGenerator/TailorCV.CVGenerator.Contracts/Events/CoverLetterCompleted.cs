namespace TailorCV.CVGenerator.Contracts.Events;

public record CoverLetterCompleted(
    Guid GenerationId,
    string CoverLetter);
