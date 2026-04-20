using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public class Skill : Entity
{
    public Guid ProfileId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string[] Items { get; private set; } = [];

    private Skill() { }

    public static Result<Skill> Create(
        Guid profileId,
        string category,
        string[] items)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return Result<Skill>.Failure(Error.Validation("Category is required"));
        }

        if (items is null || items.Length == 0)
        {
            return Result<Skill>.Failure(Error.Validation("At least one skill item is required"));
        }

        return Result<Skill>.Success(new Skill
        {
            ProfileId = profileId,
            Category = category.Trim(),
            Items = items,
        });
    }

    public void Update(string category, string[] items)
    {
        Category = category.Trim();
        Items = items;
    }
}
