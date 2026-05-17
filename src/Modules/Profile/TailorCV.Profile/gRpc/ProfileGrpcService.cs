using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TailorCV.Profile.Contracts.Grpc;
using TailorCV.Profile.Infrastructure;

namespace TailorCV.Profile.gRpc;

public class ProfileGrpcService(
    ProfileDbContext dbContext) : ProfileService.ProfileServiceBase
{
    public override async Task<GetProfileByIdResponse> GetProfileById(
        GetProfileByIdRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out Guid profileId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid profile ID format"));
        }

        Domain.Profile? profile = await dbContext.Profiles
            .Include(p => p.Sections)
            .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(p => p.Id == profileId, context.CancellationToken);

#pragma warning disable IDE0270
        if (profile is null)
#pragma warning restore IDE0270
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Profile not found"));
        }

        string sectionsJson = JsonSerializer.Serialize(profile.Sections);

        return new GetProfileByIdResponse
        {
            Id = profile.Id.ToString(),
            UserId = profile.UserId.ToString(),
            Headline = profile.Headline,
            Summary = profile.Summary,
            Phone = profile.Phone,
            Location = profile.Location,
            Website = profile.Website,
            LinkedinUrl = profile.LinkedinUrl,
            GithubUrl = profile.GithubUrl,
            SectionsJson = sectionsJson,
        };
    }
}
