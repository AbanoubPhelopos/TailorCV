namespace TailorCV.Profile.Domain;

public class ParsedResumeData
{
    public string? Headline { get; set; }
    public string? Summary { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? Linkedin { get; set; }
    public string? Github { get; set; }
    public List<ParsedSection> Sections { get; set; } = [];
}

public class ParsedSection
{
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
    public List<ParsedItem> Items { get; set; } = [];
}

public class ParsedItem
{
    public int Order { get; set; }
    public string? Company { get; set; }
    public string? Role { get; set; }
    public string? Description { get; set; }
    public bool? IsCurrent { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
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
    public string? Date { get; set; }
    public string? ExpiryDate { get; set; }
    public string? LanguageName { get; set; }
    public string? Proficiency { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
}
