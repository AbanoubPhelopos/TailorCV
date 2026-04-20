using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Domain.Enums;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class RemoveSection
{
    public record Request(Guid SectionId);

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService) : ICommandHandler<Request, Result>
    {
        public async Task<Result<Result>> HandleAsync(Request command, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            Domain.Profile? profile = await dbContext.Profiles
                .FirstOrDefaultAsync(p => p.UserId == userId, ct);

            if (profile is null)
            {
                return Result<Result>.Failure(ProfileErrors.ProfileNotFound);
            }

            SectionOrder? sectionOrder = await dbContext.SectionOrders
                .FirstOrDefaultAsync(so => so.SectionId == command.SectionId, ct);

            if (sectionOrder is null)
            {
                return Result<Result>.Failure(ProfileErrors.SectionNotFound);
            }

            if (sectionOrder.ProfileId != profile.Id)
            {
                return Result<Result>.Failure(ProfileErrors.SectionNotOwned);
            }

            switch (sectionOrder.SectionType)
            {
                case SectionType.Experience:
                    Experience? exp = await dbContext.Experiences.FindAsync([command.SectionId], ct);
                    if (exp is not null)
                    {
                        dbContext.Experiences.Remove(exp);
                    }

                    break;
                case SectionType.Project:
                    Project? proj = await dbContext.Projects.FindAsync([command.SectionId], ct);
                    if (proj is not null)
                    {
                        dbContext.Projects.Remove(proj);
                    }

                    break;
                case SectionType.Skill:
                    Skill? skill = await dbContext.Skills.FindAsync([command.SectionId], ct);
                    if (skill is not null)
                    {
                        dbContext.Skills.Remove(skill);
                    }

                    break;
                case SectionType.Education:
                    Education? edu = await dbContext.Education.FindAsync([command.SectionId], ct);
                    if (edu is not null)
                    {
                        dbContext.Education.Remove(edu);
                    }

                    break;
                case SectionType.Certification:
                    Certification? cert = await dbContext.Certifications.FindAsync([command.SectionId], ct);
                    if (cert is not null)
                    {
                        dbContext.Certifications.Remove(cert);
                    }

                    break;
                case SectionType.Language:
                    Language? lang = await dbContext.Languages.FindAsync([command.SectionId], ct);
                    if (lang is not null)
                    {
                        dbContext.Languages.Remove(lang);
                    }

                    break;
                case SectionType.Custom:
                    CustomSection? custom = await dbContext.CustomSections.FindAsync([command.SectionId], ct);
                    if (custom is not null)
                    {
                        dbContext.CustomSections.Remove(custom);
                    }

                    break;
            }

            dbContext.SectionOrders.Remove(sectionOrder);

            List<SectionOrder> remainingOrders = await dbContext.SectionOrders
                .Where(so => so.ProfileId == profile.Id && so.SectionId != command.SectionId)
                .OrderBy(so => so.Order)
                .ToListAsync(ct);

            for (int i = 0; i < remainingOrders.Count; i++)
            {
                remainingOrders[i].Order = i + 1;
            }

            await dbContext.SaveChangesAsync(ct);

            return Result<Result>.Success(Result.Success());
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/profiles/me/sections/{sectionId:guid}", async (
            Guid sectionId,
            ICommandHandler<Request, Result> handler,
            CancellationToken ct) =>
        {
            Result<Result> result = await handler.HandleAsync(new Request(sectionId), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : result.ToProblemDetails();
        })
        .RequireAuthorization()
        .WithTags("Profile")
        .WithName("RemoveSection")
        .WithSummary("Remove a profile section")
        .WithDescription("Deletes a section and renumbers remaining sections.");
    }
}
