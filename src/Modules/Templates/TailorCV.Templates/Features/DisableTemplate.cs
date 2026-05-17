using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Templates.Domain;
using TailorCV.Templates.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Interfaces;
using TailorCV.Shared.Results;

namespace TailorCV.Templates.Features;

public static class DisableTemplate
{
    public record Request(Guid Id);

    public record Response(Guid Id);

    public class Handler(
        TemplatesDbContext dbContext,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Request, Response>
    {
        public async Task<Result<Response>> HandleAsync(Request command, CancellationToken ct)
        {
            Template? template = await dbContext.Templates
                .FirstOrDefaultAsync(t => t.Id == command.Id, ct);

            if (template is null)
            {
                return Result<Response>.Failure(TemplateErrors.TemplateNotFound);
            }

            template.Disable(dateTimeProvider.UtcNow);
            await dbContext.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(template.Id));
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/admin/templates/{id}", async (
            Guid id,
            ICommandHandler<Request, Response> handler,
            CancellationToken ct) =>
        {
            Result<Response> result = await handler.HandleAsync(new Request(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : result.ToProblemDetails();
        })
        .WithTags("Templates")
        .WithName("DisableTemplate")
        .WithSummary("Disable a template")
        .WithDescription("Soft-deletes a template by setting IsActive to false. Disabled templates are hidden from users but can be reactivated.")
        .Produces(204)
        .RequireAuthorization("Admin");
    }
}
