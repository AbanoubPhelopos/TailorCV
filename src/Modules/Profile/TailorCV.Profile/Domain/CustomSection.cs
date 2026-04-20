using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public class CustomSectionItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class CustomSection : Entity
{
    public Guid ProfileId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public CustomSectionItem[] Items { get; private set; } = [];

    private CustomSection() { }

    public static Result<CustomSection> Create(
        Guid profileId,
        string title,
        CustomSectionItem[] items)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result<CustomSection>.Failure(Error.Validation("Title is required"));
        }

        if (items is null || items.Length == 0)
        {
            return Result<CustomSection>.Failure(Error.Validation("At least one item is required"));
        }

        return Result<CustomSection>.Success(new CustomSection
        {
            ProfileId = profileId,
            Title = title.Trim(),
            Items = items,
        });
    }

    public void Update(string title, CustomSectionItem[] items)
    {
        Title = title.Trim();
        Items = items;
    }
}
