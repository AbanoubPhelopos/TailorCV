using TailorCV.Profile.Domain.Enums;
using TailorCV.Shared.Primitives;

namespace TailorCV.Profile.Domain;

public class ParseJob : Entity
{
    public Guid UserId { get; private set; }
    public string S3Key { get; private set; } = string.Empty;
    public ParseJobStatus Status { get; private set; }
    public ParsedResumeData? ParsedData { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ParseJob() { }

    public static ParseJob Create(Guid userId, string s3Key, DateTimeOffset now)
    {
        return new ParseJob
        {
            UserId = userId,
            S3Key = s3Key,
            Status = ParseJobStatus.Queued,
            CreatedAt = now,
        };
    }

    public void MarkProcessing()
    {
        Status = ParseJobStatus.Processing;
    }

    public void MarkDone(ParsedResumeData parsedData, DateTimeOffset now)
    {
        Status = ParseJobStatus.Done;
        ParsedData = parsedData;
        CompletedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Status = ParseJobStatus.Failed;
        Error = error;
        CompletedAt = now;
    }
}
