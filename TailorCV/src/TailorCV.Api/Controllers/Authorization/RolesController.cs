using TailorCV.Modules.Identity.Abstractions.Messaging;
using TailorCV.Modules.Identity.Authorization.Roles.Create;
using TailorCV.Modules.Identity.Authorization.Roles.Delete;
using TailorCV.Modules.Identity.Authorization.Roles.GetAll;
using TailorCV.Modules.Identity.Authorization.Roles.GetById;
using TailorCV.Modules.Identity.Authorization.Roles.Update;
using TailorCV.Modules.Identity.Authorization.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using TailorCV.SharedKernel;
using TailorCV.Api.Infrastructure;
using TailorCV.Api.Extensions;

namespace TailorCV.Api.Controllers.Authorization;

[ApiController]
[Route("roles")]
[ApiVersion("1.0")]
[EnableRateLimiting("fixed")]
public class RolesController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "roles:create")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IResult> Create(
        [FromBody] CreateRoleRequest request,
        ICommandHandler<CreateRoleCommand, RoleResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(request.Name, request.Description, request.Permissions);
        Result<RoleResponse> result = await handler.Handle(command, cancellationToken);
        return result.Match(Results.Ok, CustomResults.Problem);
    }

    [HttpGet]
    [Authorize(Policy = "roles:read")]
    [ProducesResponseType(typeof(PagedResult<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        IQueryHandler<GetRolesQuery, PagedResult<RoleResponse>> handler = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetRolesQuery(page, pageSize);
        Result<PagedResult<RoleResponse>> result = await handler.Handle(query, cancellationToken);
        return result.Match(Results.Ok, CustomResults.Problem);
    }

    [HttpGet("{roleId:guid}")]
    [Authorize(Policy = "roles:read")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetById(
        [FromRoute] Guid roleId,
        IQueryHandler<GetRoleByIdQuery, RoleResponse> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetRoleByIdQuery(roleId);
        Result<RoleResponse> result = await handler.Handle(query, cancellationToken);
        return result.Match(Results.Ok, CustomResults.Problem);
    }

    [HttpPut("{roleId:guid}")]
    [Authorize(Policy = "roles:update")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Update(
        [FromRoute] Guid roleId,
        [FromBody] UpdateRoleRequest request,
        ICommandHandler<UpdateRoleCommand, RoleResponse> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoleCommand(roleId, request.Name, request.Description, request.Permissions);
        Result<RoleResponse> result = await handler.Handle(command, cancellationToken);
        return result.Match(Results.Ok, CustomResults.Problem);
    }

    [HttpDelete("{roleId:guid}")]
    [Authorize(Policy = "roles:delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Delete(
        [FromRoute] Guid roleId,
        ICommandHandler<DeleteRoleCommand, bool> handler,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRoleCommand(roleId);
        Result<bool> result = await handler.Handle(command, cancellationToken);
        return result.Match(_ => Results.NoContent(), CustomResults.Problem);
    }
}

public sealed record CreateRoleRequest(string Name, string Description, IEnumerable<string> Permissions);
public sealed record UpdateRoleRequest(string Name, string Description, IEnumerable<string> Permissions);
