#pragma warning disable CA1054
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Templates.Domain;
using TailorCV.Templates.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Results;

namespace TailorCV.Templates.Features;

public static class GetTemplate
{
    public record Request(Guid Id);

    public record Response(
        Guid Id,
        string Name,
        string Description,
        string ThumbnailUrl,
        string HtmlContent,
        string CssContent,
        string Category,
        string Style,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public class Handler(TemplatesDbContext dbContext) : IQueryHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request query, CancellationToken ct)
        {
            Template? template = await dbContext.Templates
                .FirstOrDefaultAsync(t => t.Id == query.Id, ct);

            if (template is null)
            {
                return Result<Response>.Failure(TemplateErrors.TemplateNotFound);
            }

            if (!template.IsActive)
            {
                return Result<Response>.Failure(TemplateErrors.TemplateNotFound);
            }

            return Result<Response>.Success(new Response(
                template.Id,
                template.Name,
                template.Description,
                template.ThumbnailUrl,
                template.HtmlContent,
                template.CssContent,
                template.Category,
                template.Style,
                template.IsActive,
                template.CreatedAt,
                template.UpdatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/templates/{id}", async (
            Guid id,
            IQueryHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(new Request(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("Templates")
        .WithName("GetTemplate")
        .WithSummary("Get template details")
        .WithDescription("Returns full template details including HTML and CSS content for client-side rendering.")
        .Produces<Response>()
        .RequireAuthorization();
    }
}
