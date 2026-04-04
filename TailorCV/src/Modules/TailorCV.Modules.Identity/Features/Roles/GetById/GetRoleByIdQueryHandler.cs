using TailorCV.Modules.Identity.Abstractions.Messaging;
using TailorCV.Modules.Identity.Abstractions.Repositories;
using TailorCV.Modules.Identity.Authorization.Responses;
using TailorCV.Modules.Identity.Domain.Authorization;
using TailorCV.SharedKernel;

namespace TailorCV.Modules.Identity.Authorization.Roles.GetById;

internal sealed class GetRoleByIdQueryHandler(IRoleRepository roleRepository) 
    : IQueryHandler<GetRoleByIdQuery, RoleResponse>
{
    public async Task<Result<RoleResponse>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        Role? role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        
        if (role is null)
        {
            return Result.Failure<RoleResponse>(AuthorizationErrors.RoleNotFound(request.RoleId));
        }

        var response = new RoleResponse(
            role.Id,
            role.Name,
            role.Description,
            role.RolePermissions.Select(rp => rp.Permission.Name));

        return Result<RoleResponse>.Success(response);
    }
}
