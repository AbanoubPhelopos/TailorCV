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

    public List<ProfileSection> Sections { get; private set; } = [];

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

            if (HasSectionType("experience"))
            {
                score += 20;
            }

            if (HasSectionType("project"))
            {
                score += 10;
            }

            if (HasSectionType("skill"))
            {
                score += 15;
            }

            if (HasSectionType("education"))
            {
                score += 10;
            }

            if (HasSectionType("certification"))
            {
                score += 5;
            }

            if (HasSectionType("language"))
            {
                score += 10;
            }

            return score;
        }
    }

    private bool HasSectionType(string type)
    {
        return Sections.Any(s => s.Type == type && s.Items.Count != 0);
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
            Headline = headline,
            Summary = summary,
            Phone = phone,
            Location = location,
            Website = website,
            LinkedinUrl = linkedin,
            GithubUrl = github,
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
        Headline = headline;
        Summary = summary;
        Phone = phone ;
        Location = location ;
        Website = website ;
        LinkedinUrl = linkedin;
        GithubUrl = github ;
        UpdatedAt = now;
    }

    public void UpdateSections(List<ProfileSection> sections, DateTimeOffset now)
    {
        Sections = sections;
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
