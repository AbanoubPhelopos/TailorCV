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
using RefreshTokenEntity = TailorCV.Identity.Domain.RefreshToken;
using Wolverine;

#pragma warning disable CA1308

namespace TailorCV.Identity.Features;

public static class Register
{
    public record Request(string Email, string Password, string FirstName, string LastName);

    public record Response(Guid UserId, string AccessToken, string RefreshToken);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(256);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Must(p => p.Any(char.IsUpper))
                .WithMessage("Password must contain at least one uppercase letter")
                .Must(p => p.Any(char.IsLower))
                .WithMessage("Password must contain at least one lowercase letter")
                .Must(p => p.Any(char.IsDigit))
                .WithMessage("Password must contain at least one digit")
                .Must(p => p.Any(c => !char.IsLetterOrDigit(c)))
                .WithMessage("Password must contain at least one special character");

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
        IJwtService jwtService,
        IDateTimeProvider dateTimeProvider,
        IMessageBus bus) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            string normalizedEmail = command.Email.Trim().ToLowerInvariant();

            bool emailExists = await dbContext.Users
                .AnyAsync(u => u.Email == normalizedEmail, ct);

            if (emailExists)
            {
                return Result<Response>.Failure(IdentityErrors.EmailAlreadyExists);
            }

            string passwordHash = PasswordHasher.Hash(command.Password);

            Result<User> userResult = User.Create(
                command.Email,
                passwordHash,
                command.FirstName,
                command.LastName,
                dateTimeProvider.UtcNow);

            if (userResult.IsFailure)
            {
                return Result<Response>.Failure(userResult.Error);
            }

            User user = userResult.Value;
            RefreshTokenEntity refreshToken = user.CreateRefreshToken(dateTimeProvider.UtcNow);

            dbContext.Users.Add(user);
            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new UserRegistered(user.Id, user.FirstName, user.LastName));

            string accessToken = jwtService.GenerateAccessToken(user);

            return Result<Response>.Success(new Response(user.Id, accessToken, refreshToken.Token));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/users/{result.Value.UserId}", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("Identity")
        .WithName("Register")
        .WithSummary("Register a new user")
        .WithDescription("Creates a new user account and returns access + refresh tokens.")
        .Produces<Response>(201);
    }
}

#pragma warning restore CA1308
