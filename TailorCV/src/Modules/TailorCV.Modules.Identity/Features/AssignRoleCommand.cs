using TailorCV.Modules.Identity.Abstractions.Messaging;

namespace TailorCV.Modules.Identity.Users.AssignRole;

public record AssignRoleCommand(Guid UserId, Guid RoleId) : ICommand<bool>;