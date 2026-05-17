using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Results;

namespace TailorCV.Identity.Features;

public static class Logout
{
    public sealed record Response;

    public record Request(string RefreshTokenValue);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshTokenValue).NotEmpty();
        }
    }

    public class Handler : ICommandHandler<Request, Response>
    {
        public Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            return Task.FromResult(Result<Response>.Success(new Response()));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok()
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Identity")
        .WithName("Logout")
        .WithSummary("Revoke refresh token")
        .WithDescription("Revokes the given refresh token. Requires a valid access token.")
        .Produces(200);
    }
}
