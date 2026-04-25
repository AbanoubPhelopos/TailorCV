using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Domain;

public class Certification : Entity
{
    public Guid ProfileId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Issuer { get; private set; } = string.Empty;
    public DateOnly Date { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string Url { get; private set; } = string.Empty;

    private Certification() { }

    public static Result<Certification> Create(
        Guid profileId,
        string name,
        string issuer,
        DateOnly date,
        DateOnly? expiryDate,
        string credentialLink)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Certification>.Failure(Error.Validation("Certification name is required"));
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            return Result<Certification>.Failure(Error.Validation("Issuer is required"));
        }

        if (expiryDate.HasValue && expiryDate.Value < date)
        {
            return Result<Certification>.Failure(Error.Validation("Expiry date must be after issue date"));
        }

        return Result<Certification>.Success(new Certification
        {
            ProfileId = profileId,
            Name = name.Trim(),
            Issuer = issuer.Trim(),
            Date = date,
            ExpiryDate = expiryDate,
            Url = credentialLink ?? string.Empty,
        });
    }

    public void Update(
        string name,
        string issuer,
        DateOnly date,
        DateOnly? expiryDate,
        string credentialLink)
    {
        Name = name.Trim();
        Issuer = issuer.Trim();
        Date = date;
        ExpiryDate = expiryDate;
        Url = credentialLink ?? string.Empty;
    }
}
