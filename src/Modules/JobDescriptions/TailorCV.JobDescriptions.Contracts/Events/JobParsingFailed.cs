namespace TailorCV.JobDescriptions.Contracts.Events;

public record JobParsingFailed(Guid ParseJobId, string Error);