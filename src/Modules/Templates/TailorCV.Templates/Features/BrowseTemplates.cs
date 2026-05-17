#pragma warning disable CA1054
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Templates.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Results;

namespace TailorCV.Templates.Features;

public static class BrowseTemplates
{
    public record Request(string? Category, string? Style);

    public record ResponseItem(
        Guid Id,
        string Name,
        string Description,
        string ThumbnailUrl,
        string Category,
        string Style,
        DateTimeOffset CreatedAt);

    public class Handler(TemplatesDbContext dbContext) : IQueryHandler<Request, List<ResponseItem>>
    {
        public async Task<Result<List<ResponseItem>>> HandleAsync(Request query, CancellationToken ct)
        {
            IQueryable<Domain.Template> templatesQuery = dbContext.Templates
                .Where(t => t.IsActive);

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                templatesQuery = templatesQuery.Where(t => t.Category == query.Category);
            }

            if (!string.IsNullOrWhiteSpace(query.Style))
            {
                templatesQuery = templatesQuery.Where(t => t.Style == query.Style);
            }

            List<ResponseItem> templates = await templatesQuery
                .OrderBy(t => t.Name)
                .Select(t => new ResponseItem(
                    t.Id,
                    t.Name,
                    t.Description,
                    t.ThumbnailUrl,
                    t.Category,
                    t.Style,
                    t.CreatedAt))
                .ToListAsync(ct);

            return Result<List<ResponseItem>>.Success(templates);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/templates", async (
            string? category,
            string? style,
            IQueryHandler<Request, List<ResponseItem>> handler,
            CancellationToken ct) =>
        {
            Result<List<ResponseItem>> result = await handler.HandleAsync(new Request(category, style), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("Templates")
        .WithName("BrowseTemplates")
        .WithSummary("Browse available templates")
        .WithDescription("Returns a list of active CV templates, filterable by category and style.")
        .Produces<List<ResponseItem>>()
        .RequireAuthorization();
    }
}
