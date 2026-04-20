using TailorCV.Profile.Domain.Enums;
using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public class Language : Entity
{
    public Guid ProfileId { get; private set; }
    public string LanguageName { get; private set; } = string.Empty;
    public LanguageProficiency Proficiency { get; private set; }

    private Language() { }

    public static Result<Language> Create(
        Guid profileId,
        string languageName,
        LanguageProficiency proficiency)
    {
        if (string.IsNullOrWhiteSpace(languageName))
        {
            return Result<Language>.Failure(Error.Validation("Language name is required"));
        }

        return Result<Language>.Success(new Language
        {
            ProfileId = profileId,
            LanguageName = languageName.Trim(),
            Proficiency = proficiency,
        });
    }

    public void Update(string languageName, LanguageProficiency proficiency)
    {
        LanguageName = languageName.Trim();
        Proficiency = proficiency;
    }
}
