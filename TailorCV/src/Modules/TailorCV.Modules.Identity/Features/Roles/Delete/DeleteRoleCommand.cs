using TailorCV.Modules.Identity.Abstractions.Messaging;

namespace TailorCV.Modules.Identity.Authorization.Roles.Delete;

public record DeleteRoleCommand(Guid RoleId) : ICommand<bool>;