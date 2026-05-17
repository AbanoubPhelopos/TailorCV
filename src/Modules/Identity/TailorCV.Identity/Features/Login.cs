#pragma warning disable CA1308, CA1862

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

public static class Login
{
    public record Request(string Email, string Password);

    public record Response(Guid UserId, string AccessToken, string RefreshToken);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class Handler(
        IdentityDbContext dbContext,
        IJwtService jwtService,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            User? user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == command.Email.Trim().ToLowerInvariant(), ct);

            if (user is null || !PasswordHasher.Verify(command.Password, user.PasswordHash))
            {
                return Result<Response>.Failure(IdentityErrors.InvalidCredentials);
            }

            RefreshTokenEntity refreshToken = user.CreateRefreshToken(dateTimeProvider.UtcNow);

            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync(ct);

            string accessToken = jwtService.GenerateAccessToken(user);

            return Result<Response>.Success(new Response(user.Id, accessToken, refreshToken.Token));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
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
        .WithName("Login")
        .WithSummary("Authenticate and get tokens")
        .WithDescription("Validates credentials and returns a new access token + refresh token pair.")
        .Produces<Response>();
    }
}

#pragma warning restore CA1308, CA1862
