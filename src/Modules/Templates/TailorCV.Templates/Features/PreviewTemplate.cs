using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using TailorCV.Templates.Domain;
using TailorCV.Templates.Infrastructure;
using TailorCV.Shared.CQRS;
using TailorCV.Shared.Results;

namespace TailorCV.Templates.Features;

public static class PreviewTemplate
{
    public record Request(Guid Id);

    public class Handler(TemplatesDbContext dbContext) : IQueryHandler<Request, string>
    {
        public async Task<Result<string>> HandleAsync(Request query, CancellationToken ct)
        {
            Template? template = await dbContext.Templates
                .FirstOrDefaultAsync(t => t.Id == query.Id, ct);

            if (template is null)
            {
                return Result<string>.Failure(TemplateErrors.TemplateNotFound);
            }

            if (!template.IsActive)
            {
                return Result<string>.Failure(TemplateErrors.TemplateNotFound);
            }

            string html = template.HtmlContent
                .Replace("{{name}}", "Jane Smith")
                .Replace("{{initials}}", "JS")
                .Replace("{{headline}}", "Senior Software Engineer")
                .Replace("{{email}}", "jane.smith@email.com")
                .Replace("{{phone}}", "+1 (555) 123-4567")
                .Replace("{{location}}", "San Francisco, CA")
                .Replace("{{summary}}", "Passionate software engineer with 8+ years of experience in building scalable distributed systems using C# and Azure. Proven track record of leading cross-functional teams and delivering high-impact projects.")
                .Replace("{{role}}", "Senior Engineer")
                .Replace("{{company}}", "Tech Corp")
                .Replace("{{startDate}}", "Jan 2020")
                .Replace("{{endDate}}", "Present")
                .Replace("{{description}}", "Led development of microservices architecture serving 10M+ users. Mentored a team of 5 engineers and established code review practices.")
                .Replace("{{degree}}", "B.Sc. Computer Science")
                .Replace("{{institution}}", "University of Technology")
                .Replace("{{skill}}", "<span>C#</span><span>Azure</span><span>Docker</span><span>SQL</span><span>Kubernetes</span>");

            string fullHtml = html.Replace("</head>", $"<style>{template.CssContent}</style></head>");

            return Result<string>.Success(fullHtml);
        }
    }

    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/templates/{id}/preview", async (
            Guid id,
            IQueryHandler<Request, string> handler,
            CancellationToken ct) =>
        {
            Result<string> result = await handler.HandleAsync(new Request(id), ct);
            return result.IsSuccess
                ? Results.Text(result.Value, "text/html")
                : result.ToProblemDetails();
        })
        .WithTags("Templates")
        .WithName("PreviewTemplate")
        .WithSummary("Preview template with sample data")
        .WithDescription("Returns rendered HTML with placeholder data showing how the template looks.")
        .Produces<string>(contentType: "text/html")
        .RequireAuthorization();
    }
}
