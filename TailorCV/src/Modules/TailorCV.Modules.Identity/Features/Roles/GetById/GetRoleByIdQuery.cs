using TailorCV.Modules.Identity.Abstractions.Messaging;
using TailorCV.Modules.Identity.Authorization.Responses;

namespace TailorCV.Modules.Identity.Authorization.Roles.GetById;

public record GetRoleByIdQuery(Guid RoleId) : IQuery<RoleResponse>;
