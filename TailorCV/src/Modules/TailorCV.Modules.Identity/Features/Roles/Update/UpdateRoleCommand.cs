using TailorCV.Modules.Identity.Abstractions.Messaging;
using TailorCV.Modules.Identity.Authorization.Responses;

namespace TailorCV.Modules.Identity.Authorization.Roles.Update;

public record UpdateRoleCommand(Guid RoleId, string Name, string Description, IEnumerable<string> Permissions)
    : ICommand<RoleResponse>;
