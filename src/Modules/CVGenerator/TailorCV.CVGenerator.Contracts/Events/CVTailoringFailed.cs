namespace TailorCV.CVGenerator.Contracts.Events;

public record CVTailoringFailed(
    Guid GenerationId,
    string Error);
