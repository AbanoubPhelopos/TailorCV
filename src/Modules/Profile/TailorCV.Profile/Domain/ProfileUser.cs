using TailorCV.Shared.Primitives;

namespace TailorCV.Profile.Domain;

public class ProfileUser : Entity
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    private ProfileUser() { }

    public static ProfileUser Create(Guid userId, string firstName, string lastName)
    {
        return new ProfileUser
        {
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
        };
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }
}
