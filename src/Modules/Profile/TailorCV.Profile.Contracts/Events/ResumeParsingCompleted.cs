using TailorCV.Profile.Contracts.Dto;

namespace TailorCV.Profile.Contracts.Events;

public record ResumeParsingCompleted(Guid ParseJobId, ParsedResumeData Data);
