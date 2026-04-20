using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Profile.Domain;
using TailorCV.Profile.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Profile.Features;

public static class ReorderSections
{
    public record SectionOrderItem(Guid SectionId, int Order);

    public record Request(List<SectionOrderItem> Orders);

    public record SectionOrderResponse(string SectionType, Guid SectionId, int Order);

    public record Response(List<SectionOrderResponse> Orders);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Orders)
                .NotEmpty()
                .WithMessage("Orders must have at least 1 item");

            RuleFor(x => x.Orders)
                .Must(orders => orders.Select(o => o.SectionId).Distinct().Count() == orders.Count)
                .WithMessage("Duplicate section IDs are not allowed");

            RuleFor(x => x.Orders)
                .Must(orders => orders.Select(o => o.Order).Distinct().Count() == orders.Count)
                .WithMessage("Duplicate order values are not allowed");

            RuleFor(x => x.Orders)
                .Must(BeSequential)
                .WithMessage("Orders must be sequential starting from 1");
        }

        private static bool BeSequential(List<SectionOrderItem> orders)
        {
            List<int> sorted = orders.Select(o => o.Order).OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i] != i + 1)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class Handler(
        ProfileDbContext dbContext,
        ICurrentUserService currentUserService) : ICommandHandler<Request, Response>
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

            List<SectionOrder> currentOrders = await dbContext.SectionOrders
                .Where(so => so.ProfileId == profile.Id)
                .ToListAsync(ct);

            if (command.Orders.Count != currentOrders.Count)
            {
                return Result<Response>.Failure(ProfileErrors.NotAllSectionsIncluded);
            }

            HashSet<Guid> currentIds = currentOrders.Select(so => so.SectionId).ToHashSet();
            foreach (SectionOrderItem item in command.Orders)
            {
                if (!currentIds.Contains(item.SectionId))
                {
                    return Result<Response>.Failure(ProfileErrors.InvalidSectionIds);
                }
            }

            foreach (SectionOrderItem item in command.Orders)
            {
                SectionOrder order = currentOrders.First(so => so.SectionId == item.SectionId);
                order.Order = item.Order;
            }

            await dbContext.SaveChangesAsync(ct);

            List<SectionOrderResponse> response = currentOrders
                .OrderBy(o => o.Order)
                .Select(o => new SectionOrderResponse(o.SectionType.ToString(), o.SectionId, o.Order))
                .ToList();

            return Result<Response>.Success(new Response(response));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/api/profiles/me/sections/reorder", async (
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
        .WithName("ReorderSections")
        .WithSummary("Reorder profile sections")
        .WithDescription("Reorders all profile sections. Must include all section IDs with sequential orders starting from 1.");
    }
}
