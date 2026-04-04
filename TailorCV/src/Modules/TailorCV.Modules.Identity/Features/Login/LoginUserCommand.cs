using TailorCV.Modules.Identity.Abstractions.Messaging;

namespace TailorCV.Modules.Identity.Users.Login;

public sealed record LoginUserCommand(string Email, string Password) : ICommand<string>;
