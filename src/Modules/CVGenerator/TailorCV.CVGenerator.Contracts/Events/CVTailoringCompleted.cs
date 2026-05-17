namespace TailorCV.CVGenerator.Contracts.Events;

public record CVTailoringCompleted(
    Guid GenerationId,
    string Content,
    string MatchScore,
    string? CoverLetter);
