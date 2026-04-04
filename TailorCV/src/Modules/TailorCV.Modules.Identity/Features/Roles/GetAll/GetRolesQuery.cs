using TailorCV.Modules.Identity.Abstractions.Messaging;
using TailorCV.Modules.Identity.Authorization.Responses;

namespace TailorCV.Modules.Identity.Authorization.Roles.GetAll;

public record GetRolesQuery(int Page = 1, int PageSize = 10) : IPagedQuery<PagedResult<RoleResponse>>;
