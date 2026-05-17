#pragma warning disable CA1308
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TailorCV.CVGenerator.Contracts.Dto;
using TailorCV.CVGenerator.Domain;
using TailorCV.CVGenerator.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Pagination;
using TailorCV.Shared.Results;

namespace TailorCV.CVGenerator.Features;

public static class ListHistory
{
    public record Request(int Page = 1, int PageSize = 10, string SortBy = "date", string SortOrder = "desc");

    public record ResponseItem(
        Guid Id,
        string GenerationType,
        string JobTitle,
        string Company,
        Guid TemplateId,
        int MatchScore,
        bool HasCoverLetter,
        string PdfStatus,
        string Status,
        DateTimeOffset CreatedAt);

    public class Handler(
        CVGeneratorDbContext dbContext,
        ICurrentUserService currentUserService) : IQueryHandler<Request, OffsetPagedList<ResponseItem>>
    {
        public async Task<Result<OffsetPagedList<ResponseItem>>> HandleAsync(Request query, CancellationToken ct)
        {
            Guid userId = currentUserService.UserId;

            IQueryable<GeneratedCV> queryable = dbContext.GeneratedCVs
                .Where(c => c.UserId == userId);

            queryable = query.SortBy.ToLowerInvariant() switch
            {
                "score" => query.SortOrder == "asc"
                    ? queryable.OrderBy(c => c.MatchScore)
                    : queryable.OrderByDescending(c => c.MatchScore),
                _ => query.SortOrder == "asc"
                    ? queryable.OrderBy(c => c.CreatedAt)
                    : queryable.OrderByDescending(c => c.CreatedAt)
            };

            PagingParams paging = new(query.Page, query.PageSize);
            OffsetPagedList<GeneratedCV> paged = await queryable.ToOffsetPagedListAsync(paging, ct);

            List<ResponseItem> items = paged.Items.Select(cv =>
            {
                JobSnapshotData? jobData = DeserializeJobSnapshot(cv.JobSnapshot);
                int matchScore = DeserializeMatchScore(cv.MatchScore);

                return new ResponseItem(
                    cv.Id,
                    cv.GenerationType.ToString(),
                    jobData?.Title ?? "Unknown",
                    jobData?.Company ?? "Unknown",
                    cv.TemplateId,
                    matchScore,
                    cv.CoverLetter is not null,
                    cv.PdfStatus.ToString(),
                    cv.Status.ToString(),
                    cv.CreatedAt);
            }).ToList();

            OffsetPagedList<ResponseItem> result = new(items, paging.Page, paging.PageSize, paged.PagingInfo.Total);
            return Result<OffsetPagedList<ResponseItem>>.Success(result);
        }

        private static JobSnapshotData? DeserializeJobSnapshot(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<JobSnapshotData>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static int DeserializeMatchScore(string? json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return 0;
            }

            try
            {
                MatchScoreData? score = JsonSerializer.Deserialize<MatchScoreData>(json);
                return score?.Percentage ?? 0;
            }
            catch (JsonException)
            {
                return 0;
            }
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/cv", async (
            int page,
            int pageSize,
            string sortBy,
            string sortOrder,
            IQueryHandler<Request, OffsetPagedList<ResponseItem>> handler,
            CancellationToken ct) =>
        {
            Result<OffsetPagedList<ResponseItem>> result = await handler.HandleAsync(
                new Request(page, pageSize, sortBy, sortOrder), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("CVGenerator")
        .WithName("ListHistory")
        .WithSummary("List generated CVs")
        .WithDescription("Paginated list of user's generated CVs, ordered by creation date or match score.")
        .Produces<OffsetPagedList<ResponseItem>>()
        .RequireAuthorization();
    }
}
