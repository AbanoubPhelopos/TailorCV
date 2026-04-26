using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.JobDescriptions.Domain.Enums;
using TailorCV.JobDescriptions.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Pagination;
using TailorCV.Shared.Results;

namespace TailorCV.JobDescriptions.Features;

public static class ListJobs
{
    public record Request(PagingParams Paging, string SortBy = "date", string SortOrder = "desc");

    public record ResponseItem(
        Guid Id,
        string Title,
        string Company,
        string? Location,
        string? Label,
        SeniorityLevel? SeniorityLevel,
        DateTimeOffset CreatedAt);

    public class Handler(JobDescriptionsDbContext dbContext, ICurrentUserService currentUser)
        : IQueryHandler<Request, OffsetPagedList<ResponseItem>>
    {
        public async Task<Result<OffsetPagedList<ResponseItem>>> HandleAsync(Request query, CancellationToken ct)
        {
            IQueryable<Domain.JobDescription> jobsQuery = dbContext.JobDescriptions
                .Where(j => j.UserId == currentUser.UserId);

            jobsQuery = (query.SortBy, query.SortOrder) switch
            {
                ("title", "asc") => jobsQuery.OrderBy(j => j.Title).ThenBy(j => j.Id),
                ("title", _) => jobsQuery.OrderByDescending(j => j.Title).ThenBy(j => j.Id),
                ("company", "asc") => jobsQuery.OrderBy(j => j.Company).ThenBy(j => j.Id),
                ("company", _) => jobsQuery.OrderByDescending(j => j.Company).ThenBy(j => j.Id),
                (_, "asc") => jobsQuery.OrderBy(j => j.CreatedAt).ThenBy(j => j.Id),
                _ => jobsQuery.OrderByDescending(j => j.CreatedAt).ThenBy(j => j.Id),
            };

            OffsetPagedList<ResponseItem> pagedList = await jobsQuery
                .Select(j => new ResponseItem(
                    j.Id,
                    j.Title,
                    j.Company,
                    j.Location,
                    j.Label,
                    j.SeniorityLevel,
                    j.CreatedAt))
                .ToOffsetPagedListAsync(query.Paging, ct);

            return Result<OffsetPagedList<ResponseItem>>.Success(pagedList);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/jobs", async (
            int page,
            int pageSize,
            string? sortBy,
            string? sortOrder,
            IQueryHandler<Request, OffsetPagedList<ResponseItem>> handler,
            CancellationToken ct) =>
        {
            Result<OffsetPagedList<ResponseItem>> result = await handler.HandleAsync(
                new Request(new PagingParams(page, pageSize), sortBy ?? "date", sortOrder ?? "desc"), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("JobDescription")
        .WithName("ListJobs")
        .WithSummary("List saved job descriptions")
        .WithDescription("Returns a paginated list of the user's saved job descriptions.")
        .RequireAuthorization();
    }
}
