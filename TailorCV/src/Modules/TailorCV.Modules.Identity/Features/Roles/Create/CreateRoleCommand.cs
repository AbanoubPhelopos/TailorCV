using TailorCV.Modules.Identity.Abstractions.Messaging;
using TailorCV.Modules.Identity.Authorization.Responses;

namespace TailorCV.Modules.Identity.Authorization.Roles.Create;

public record CreateRoleCommand(string Name, string Description, IEnumerable<string> Permissions)
    : ICommand<RoleResponse>;
