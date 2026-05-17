#pragma warning disable CA1054
using TailorCV.Shared.Primitives;

namespace TailorCV.Templates.Domain;

public class Template : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string HtmlContent { get; private set; } = string.Empty;
    public string CssContent { get; private set; } = string.Empty;
    public string ThumbnailUrl { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Style { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Template() { }

    public static Template Create(
        string name,
        string description,
        string htmlContent,
        string cssContent,
        string thumbnailUrl,
        string category,
        string style,
        DateTimeOffset now)
    {
        return new Template
        {
            Name = name,
            Description = description,
            HtmlContent = htmlContent,
            CssContent = cssContent,
            ThumbnailUrl = thumbnailUrl,
            Category = category,
            Style = style,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void Update(
        string name,
        string description,
        string htmlContent,
        string cssContent,
        string thumbnailUrl,
        string category,
        string style,
        bool isActive,
        DateTimeOffset now)
    {
        Name = name;
        Description = description;
        HtmlContent = htmlContent;
        CssContent = cssContent;
        ThumbnailUrl = thumbnailUrl;
        Category = category;
        Style = style;
        IsActive = isActive;
        UpdatedAt = now;
    }

    public void Disable(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }
}
