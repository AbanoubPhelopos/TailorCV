namespace TailorCV.Profile.Domain;

public class ProfileSection
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? Title { get; set; }
    public List<SectionItem> Items { get; set; } = [];
}

public class SectionItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public int Order { get; set; }

    public string? Company { get; set; }
    public string? Role { get; set; }
    public string? Description { get; set; }
    public bool IsCurrent { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public string? Name { get; set; }
    public List<string>? TechStack { get; set; }
    public string? Url { get; set; }

    public string? Category { get; set; }
    public List<string>? SkillItems { get; set; }

    public string? Institution { get; set; }
    public string? Degree { get; set; }
    public string? Field { get; set; }
    public string? Gpa { get; set; }

    public string? Issuer { get; set; }
    public DateOnly? Date { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public string? LanguageName { get; set; }
    public string? Proficiency { get; set; }

    public string? Title { get; set; }
    public string? Subtitle { get; set; }
}
