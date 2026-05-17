namespace TailorCV.CVGenerator.Contracts.Commands;

public record TailorCoverLetter(
    Guid GenerationId,
    string ProfileSnapshot,
    string JobSnapshot,
    string? TailoringPrompt);
