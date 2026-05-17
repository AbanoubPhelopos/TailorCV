namespace TailorCV.CVGenerator.Contracts.Commands;

public record ExportCvPdf(
    Guid GenerationId,
    Guid TemplateId,
    string Content,
    string ProfileSnapshot,
    string JobSnapshot);
