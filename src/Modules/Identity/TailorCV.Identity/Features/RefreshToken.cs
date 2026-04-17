using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Identity.Domain;
using TailorCV.Identity.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using RefreshTokenEntity = TailorCV.Identity.Domain.RefreshToken;

namespace TailorCV.Identity.Features;

public static class RefreshToken
{
    public record Request(string RefreshTokenValue);

    public record Response(Guid UserId, string AccessToken, string RefreshToken);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.RefreshTokenValue).NotEmpty();
        }
    }

    public class Handler(
        IdentityDbContext dbContext,
        IJwtService jwtService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            RefreshTokenEntity? existingToken = await dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == command.RefreshTokenValue, ct);

            if (existingToken is null)
            {
                return Result<Response>.Failure(IdentityErrors.RefreshTokenNotFound);
            }

            if (existingToken.IsExpired(dateTimeProvider.UtcNow))
            {
                return Result<Response>.Failure(IdentityErrors.RefreshTokenExpired);
            }

            if (existingToken.User is null)
            {
                return Result<Response>.Failure(IdentityErrors.UserDeleted);
            }

            RefreshTokenEntity newRefreshToken = existingToken.User.CreateRefreshToken(dateTimeProvider.UtcNow);

            dbContext.RefreshTokens.Remove(existingToken);
            dbContext.RefreshTokens.Add(newRefreshToken);
            await dbContext.SaveChangesAsync(ct);

            string accessToken = jwtService.GenerateAccessToken(existingToken.User);

            return Result<Response>.Success(new Response(existingToken.User.Id, accessToken, newRefreshToken.Token));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", async (
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
        .WithName("RefreshToken")
        .WithSummary("Rotate refresh token")
        .WithDescription("Exchanges a valid refresh token for a new access token + refresh token pair. The old token is revoked.");
    }
}
