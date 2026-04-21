using TailorCV.JobDescriptions.Contracts.Dto;

namespace TailorCV.JobDescriptions.Contracts.Events;

#pragma warning disable CA1054
public record JobParsingCompleted(
    Guid ParseJobId,
    ParsedJobDataDto Data,
    string? RawText = null,
    Uri? SourceUrl = null);
#pragma warning restore CA1054
