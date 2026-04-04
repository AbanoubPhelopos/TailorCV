using TailorCV.Modules.Identity.Abstractions.Messaging;

namespace TailorCV.Modules.Identity.Users.Register;

public sealed record RegisterUserCommand(string Email, string FirstName, string LastName, string Password)
    : ICommand<Guid>;
