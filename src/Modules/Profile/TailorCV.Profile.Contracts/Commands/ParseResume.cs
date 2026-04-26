namespace TailorCV.Profile.Contracts.Commands;

public record ParseResume(Guid ParseJobId, string S3Key, string FileName, string ContentType, Guid UserId);
