#pragma warning disable CA1308

using TailorCV.Identity.Domain.Enums;
using TailorCV.Shared.Primitives;
using TailorCV.Shared.Results;

namespace TailorCV.Identity.Domain;

public class User : Entity
{
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    private User() { }

    public static Result<User> Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<User>.Failure(Error.Validation("Email is required"));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result<User>.Failure(Error.Validation("Password hash is required"));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result<User>.Failure(Error.Validation("First name is required"));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result<User>.Failure(Error.Validation("Last name is required"));
        }

        return Result<User>.Success(new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Role = UserRole.User,
            CreatedAt = now,
        });
    }

    public RefreshToken CreateRefreshToken(DateTimeOffset now) =>
        RefreshToken.Create(Id, now);

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }
}

#pragma warning restore CA1308
