using TailorCV.JobScraper.Domain.Enums;
using TailorCV.Shared.Primitives;

namespace TailorCV.JobScraper.Domain;

public class ParseJob : Entity
{
    public Guid UserId { get; private set; }
    public ParseJobType Type { get; private set; }
    public string RawInput { get; private set; } = string.Empty;
    public ParseJobStatus Status { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public ParsedJobData? ParsedData { get; private set; }

    private ParseJob() { }

    public static ParseJob Create(
        Guid userId,
        ParseJobType type,
        string rawInput,
        DateTimeOffset now)
    {
        return new ParseJob
        {
            UserId = userId,
            Type = type,
            RawInput = rawInput,
            Status = ParseJobStatus.Queued,
            CreatedAt = now
        };
    }

    public void MarkProcessing() => Status = ParseJobStatus.Processing;

    public void MarkDone(ParsedJobData parsedData, DateTimeOffset now)
    {
        ParsedData = parsedData;
        Status = ParseJobStatus.Done;
        CompletedAt = now;
    }

    public void MarkFailed(string error, DateTimeOffset now)
    {
        Error = error;
        Status = ParseJobStatus.Failed;
        CompletedAt = now;
    }

    public bool IsOwner(Guid userId) => UserId == userId;
}

public record ParsedJobData(
    string Title,
    string? Company,
    string? Location,
    List<string> RequiredSkills,
    List<string> Responsibilities,
    List<string> Qualifications,
    string SeniorityLevel
);
