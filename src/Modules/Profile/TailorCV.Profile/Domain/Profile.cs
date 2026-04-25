using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public class Profile : Entity
{
    public Guid UserId { get; private set; }
    public string Headline { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public string Website { get; private set; } = string.Empty;
    public string LinkedinUrl { get; private set; } = string.Empty;
    public string GithubUrl { get; private set; } = string.Empty;
    public string? ShareId { get; private set; }
    public bool IsShared { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ICollection<Experience> Experiences { get; private set; } = [];
    public ICollection<Project> Projects { get; private set; } = [];
    public ICollection<Skill> Skills { get; private set; } = [];
    public ICollection<Education> Education { get; private set; } = [];
    public ICollection<Certification> Certifications { get; private set; } = [];
    public ICollection<Language> Languages { get; private set; } = [];
    public ICollection<CustomSection> CustomSections { get; private set; } = [];
    public ICollection<SectionOrder> SectionOrders { get; private set; } = [];

    private Profile() { }

    public int Completeness
    {
        get
        {
            int score = 0;

            if (!string.IsNullOrWhiteSpace(Headline))
            {
                score += 10;
            }

            if (!string.IsNullOrWhiteSpace(Summary) && Summary.Length >= 50)
            {
                score += 10;
            }

            if (!string.IsNullOrWhiteSpace(Phone))
            {
                score += 5;
            }

            if (!string.IsNullOrWhiteSpace(Location))
            {
                score += 5;
            }

            if (Experiences.Count != 0)
            {
                score += 20;
            }

            if (Projects.Count != 0)
            {
                score += 10;
            }

            if (Skills.Count != 0)
            {
                score += 15;
            }

            if (Education.Count != 0)
            {
                score += 10;
            }

            if (Certifications.Count != 0)
            {
                score += 5;
            }

            if (Languages.Count != 0)
            {
                score += 10;
            }

            return score;
        }
    }

    public static Result<Profile> Create(
        Guid userId,
        string headline,
        string summary,
        string phone,
        string location,
        string website,
        string linkedin,
        string github,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            return Result<Profile>.Failure(Error.Validation("User ID is required"));
        }

        return Result<Profile>.Success(new Profile
        {
            UserId = userId,
            Headline = headline ?? string.Empty,
            Summary = summary ?? string.Empty,
            Phone = phone ?? string.Empty,
            Location = location ?? string.Empty,
            Website = website ?? string.Empty,
            LinkedinUrl = linkedin ?? string.Empty,
            GithubUrl = github ?? string.Empty,
            IsShared = false,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public void Update(
        string headline,
        string summary,
        string phone,
        string location,
        string website,
        string linkedin,
        string github,
        DateTimeOffset now)
    {
        Headline = headline ?? string.Empty;
        Summary = summary ?? string.Empty;
        Phone = phone ?? string.Empty;
        Location = location ?? string.Empty;
        Website = website ?? string.Empty;
        LinkedinUrl = linkedin ?? string.Empty;
        GithubUrl = github ?? string.Empty;
        UpdatedAt = now;
    }

    public void EnableSharing(string shareId)
    {
        ShareId ??= shareId;
        IsShared = true;
    }

    public void DisableSharing()
    {
        IsShared = false;
    }
}
