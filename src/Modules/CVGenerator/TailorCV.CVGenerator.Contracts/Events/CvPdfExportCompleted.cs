namespace TailorCV.CVGenerator.Contracts.Events;

public record CvPdfExportCompleted(
    Guid GenerationId,
    string PdfKey);
