#pragma warning disable CA1054
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using TailorCV.Templates.Domain;
using TailorCV.Templates.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Templates.Features;

public static class CreateTemplate
{
    public record Request(
        string Name,
        string Description,
        string HtmlContent,
        string CssContent,
        string ThumbnailUrl,
        string Category,
        string Style);

    public record Response(
        Guid Id,
        string Name,
        string Description,
        string ThumbnailUrl,
        string Category,
        string Style,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    public class Validator : AbstractValidator<Request>
    {
        private static readonly HashSet<string> ValidCategories = ["minimal", "professional", "creative"];
        private static readonly HashSet<string> ValidStyles = ["modern", "classic", "bold"];

        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);
            RuleFor(x => x.HtmlContent).NotEmpty().MinimumLength(50);
            RuleFor(x => x.CssContent).NotEmpty().MinimumLength(10);
            RuleFor(x => x.ThumbnailUrl).NotEmpty().MaximumLength(2048);
            RuleFor(x => x.Category).NotEmpty().Must(c => ValidCategories.Contains(c))
                .WithMessage("Category must be one of: minimal, professional, creative");
            RuleFor(x => x.Style).NotEmpty().Must(s => ValidStyles.Contains(s))
                .WithMessage("Style must be one of: modern, classic, bold");
        }
    }

    public class Handler(
        TemplatesDbContext dbContext,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            DateTimeOffset now = dateTimeProvider.UtcNow;

            Template template = Template.Create(
                command.Name,
                command.Description,
                command.HtmlContent,
                command.CssContent,
                command.ThumbnailUrl,
                command.Category,
                command.Style,
                now);

            dbContext.Templates.Add(template);
            await dbContext.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                template.Id,
                template.Name,
                template.Description,
                template.ThumbnailUrl,
                template.Category,
                template.Style,
                template.IsActive,
                template.CreatedAt,
                template.UpdatedAt));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/templates", async (
            Request request,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(request, ct);
            return result.IsSuccess
                ? Results.Created($"/api/templates/{result.Value.Id}", result.Value)
                : result.ToProblemDetails();
        })
        .WithTags("Templates")
        .WithName("CreateTemplate")
        .WithSummary("Create a new template")
        .WithDescription("Creates a new CV template with HTML/CSS content, metadata, and thumbnail.")
        .Produces<Response>(201)
        .RequireAuthorization("Admin");
    }
}
