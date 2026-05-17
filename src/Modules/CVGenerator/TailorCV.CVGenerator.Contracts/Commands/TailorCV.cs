namespace TailorCV.CVGenerator.Contracts.Commands;

public record TailorCV(
    Guid GenerationId,
    Guid UserId,
    string ProfileSnapshot,
    string JobSnapshot,
    Guid TemplateId,
    bool IncludeCoverLetter,
    string? TailoringPrompt);
