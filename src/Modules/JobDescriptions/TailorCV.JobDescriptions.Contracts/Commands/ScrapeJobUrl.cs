namespace TailorCV.JobDescriptions.Contracts.Commands;

public record ScrapeJobUrl(Guid ParseJobId, Uri SourceUrl);
