using TailorCV.Shared.Primitives;

namespace TailorCV.Identity.Domain;

public class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = null!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public User User { get; private set; } = null!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, DateTimeOffset now)
    {
        return new RefreshToken
        {
            UserId = userId,
            Token = Guid.CreateVersion7().ToString(),
            ExpiresAt = now.AddDays(7),
            CreatedAt = now,
        };
    }

    public bool IsExpired(DateTimeOffset now) =>
        ExpiresAt < now;
}
