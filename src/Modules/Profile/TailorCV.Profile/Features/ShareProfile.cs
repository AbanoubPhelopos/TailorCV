using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Contracts.Events;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;
using Wolverine;

namespace TailorCV.Profile.Features;

public static class ShareProfile
{
    public record Request(bool Enabled);

    public record Response(bool IsShared, string? ShareLink, string? ShareId);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService,
        IMessageBus bus) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            Domain.Profile? profile = await dbContext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                return Result<Response>.Failure(ProfileErrors.ProfileNotFound);
            }

            if (command.Enabled)
            {
                string shareId = GenerateShareId();
                profile.EnableSharing(shareId);
            }
            else
            {
                profile.DisableSharing();
            }

            await dbContext.SaveChangesAsync(ct);

            await bus.PublishAsync(new ProfileUpdated(userId, profile.Id, profile.UpdatedAt));

            return Result<Response>.Success(new Response(
                profile.IsShared,
                profile.IsShared ? $"/api/profiles/shared/{profile.ShareId}" : null,
                profile.IsShared ? profile.ShareId : null));
        }

        private static string GenerateShareId()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(15);
            return Base64UrlTextEncoder.Encode(bytes);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/profiles/me/share", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("ShareProfile")
        .WithSummary("Toggle profile sharing")
        .WithDescription("Enables or disables profile sharing. Generates a unique share URL on first enable.")
        .Produces<Response>();
    }
}
