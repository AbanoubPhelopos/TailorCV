namespace TailorCV.Profile.Contracts.Events;

public record ResumeParsingFailed(Guid ParseJobId, string Error);
