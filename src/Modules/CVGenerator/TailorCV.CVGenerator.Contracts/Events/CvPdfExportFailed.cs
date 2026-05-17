namespace TailorCV.CVGenerator.Contracts.Events;

public record CvPdfExportFailed(
    Guid GenerationId,
    string Error);
