using TailorCV.JobDescriptions.Domain.Enums;
using TailorCV.Shared.Primitives;

namespace TailorCV.JobDescriptions.Domain;

public class JobDescription : Entity
{
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Company { get; private set; } = string.Empty;
    public string? Location { get; private set; }
    public List<string> RequiredSkills { get; private set; } = [];
    public List<string> Responsibilities { get; private set; } = [];
    public List<string> Qualifications { get; private set; } = [];
    public SeniorityLevel? SeniorityLevel { get; private set; }
    public Uri? SourceUrl { get; private set; } 
    public string? Label { get; private set; }
    public string? RawText { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private JobDescription() { }

    public static JobDescription Create(
        Guid userId,
        string title,
        string company,
        string? location,
        List<string>? requiredSkills,
        List<string>? responsibilities,
        List<string>? qualifications,
        SeniorityLevel? seniorityLevel,
        Uri? sourceUrl,
        string? label,
        string? rawText,
        DateTimeOffset now)
    {
        return new JobDescription
        {
            UserId = userId,
            Title = title,
            Company = company,
            Location = location,
            RequiredSkills = requiredSkills ?? [],
            Responsibilities = responsibilities ?? [],
            Qualifications = qualifications ?? [],
            SeniorityLevel = seniorityLevel,
            SourceUrl = sourceUrl,
            Label = label,
            RawText = rawText,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool IsOwner(Guid userId) => UserId == userId;
}
