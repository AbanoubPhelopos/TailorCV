namespace TailorCV.JobDescriptions.Contracts.Commands;

public record ParseJobText(Guid ParseJobId, string RawText, Uri? SourceUrl = null);
