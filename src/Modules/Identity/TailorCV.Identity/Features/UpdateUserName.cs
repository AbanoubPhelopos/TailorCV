using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Identity.Contracts.Events;
using TailorCV.Identity.Domain;
using TailorCV.Identity.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using Wolverine;

namespace TailorCV.Identity.Features;

public static class UpdateUserName
{
    public record Request(string FirstName, string LastName);

    public record Response(Guid UserId, string FirstName, string LastName);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(100);
        }
    }

    public class Handler(
        IdentityDbContext dbContext,
        ICurrentUserService currentUserService,
        IMessageBus bus) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            User? user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return Result<Response>.Failure(IdentityErrors.UserNotFound);
            }

            user.UpdateName(command.FirstName, command.LastName);
            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new UserNameUpdated(user.Id, user.FirstName, user.LastName));

            return Result<Response>.Success(new Response(user.Id, user.FirstName, user.LastName));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/auth/user/name", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("Identity")
        .WithName("UpdateUserName")
        .WithSummary("Update user name")
        .WithDescription("Updates the authenticated user's first and last name.")
        .Produces<Response>()
        .RequireAuthorization();
    }
}
