using TailorCV.Modules.Identity.Domain.Authorization;
using TailorCV.SharedKernel;

namespace TailorCV.Modules.Identity.Domain;

public sealed class User : Entity
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PasswordHash { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
